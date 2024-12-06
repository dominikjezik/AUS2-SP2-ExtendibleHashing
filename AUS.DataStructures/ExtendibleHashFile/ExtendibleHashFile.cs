using System.Collections;

namespace AUS.DataStructures.ExtendibleHashFile;

public class ExtendibleHashFile<TRecord> where TRecord : IEhfRecord, new()
{
    private int _blockSize;
    
    private EhfBlock<TRecord> _loadedBlock;
    
    private FileStream _fileStream;
    
    private bool _isLoadedControlBlock = false;
    
    private long _firstFreeBlockAddress = -1;
    
    private EhfDirectory _directory;
    
    public ExtendibleHashFile(string fileName, int blockSize)
    {
        _blockSize = blockSize;
        
        _loadedBlock = new EhfBlock<TRecord>(blockSize);
        _fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        
        _directory = LoadDirectory();
    }
    
    public void Close()
    {
        // Ak sa v subore nenachadza ziadny zaznam, odstrani sa riadiaci blok a adresar
        if (_fileStream.Length == _blockSize)
        {
            _fileStream.SetLength(0);
            _fileStream.Close();
            
            _directory.DeleteFile();
            
            return;
        }
        
        // Ak v subore nie je nic, odstrani sa adresar
        if (_fileStream.Length == 0)
        {
            _fileStream.Close();
            
            _directory.DeleteFile();
            
            return;
        }
        
        // Zapis riadiaceho bloku
        if (_isLoadedControlBlock)
        {
            WriteControlBlock();
        }
        
        // Flushnutie a zatvorenie suboru
        _fileStream.Flush();
        _fileStream.Close();
        
        // Zapis adresara na disk
        _directory?.SaveToFile();
    }
    
    public TRecord? Get(TRecord recordWithKey)
    {
        var hash = recordWithKey.GetHash();
        var blockAddress = _directory.GetAddress(hash);
        
        if (blockAddress == -1)
        {
            return default;
        }
        
        LoadBlockFromAddress(blockAddress);
        
        return _loadedBlock.Get(recordWithKey);
    }

    #region InsertOperations
    
    public void Insert(TRecord recordToInsert)
    {
        // Ak v subore nie je nic, treba vytvorit riadiaci blok a prvy blok + sa nastavi adresa v adresari
        if (_fileStream.Length == 0)
        {
            long newBlockAddress = WriteFirstBlockToEmptyFile(recordToInsert);
            _directory.SetDirectoryItems(recordToInsert.GetHash(), newBlockAddress, 0, 1);
            return;
        }
        
        // Ak sme doteraz nenacitali riadiaci blok, nacitame ho
        if (!_isLoadedControlBlock)
        {
            LoadControlBlock();
        }
        
        // Pokial sa zaznam nepodarilo vlozit:
        // 1. Vypočítaj hash -> bitové pole
        // 2. Zober d bitov z hashu
        // 3. Preveď na int a získaj adresu z adresára
        // 4. Načítaj blok na danej adrese
        // 5. Ak blok nie je plný, vlož záznam do bloku, zapíš blok a koniec
        //    Ak je blok plný:
        // 6. Ak Depth bloku = Depth adresára, zdvojnasov adresár
        // 7. Rozdelenie bloku - Split - vytvorenie noveho bloku (zaznamy sa rozdelia)
        // 8. Opakuj krok 1
        
        var inserted = false;
        var fullBlockOfRecordsIsAlreadyLoaded = false;
        
        while (!inserted)
        {
            var hash = recordToInsert.GetHash();
            var directoryItem = _directory.GetDirectoryItem(hash);
            
            var blockAddress = directoryItem.BlockAddress;

            if (blockAddress != -1)
            {
                // Ak bol uz plny blok nacitany z minulej iteracie, tak ho netreba nacitat znova
                if (!fullBlockOfRecordsIsAlreadyLoaded)
                {
                    LoadBlockFromAddress(blockAddress);
                }
            }
            else
            {
                // Tuto je potrebne vlozit novy blok
                var block = new EhfBlock<TRecord>(_blockSize);
                block.Insert(recordToInsert);
                
                blockAddress = WriteToFreeBlock(block);
                
                _directory.SetDirectoryItems(hash, blockAddress, directoryItem.Depth, 1);

                return;
            }
            
            // Kontrola ci uz nebol vlozeny takyto zaznam (nepodporujeme duplicity)
            if (_loadedBlock.Get(recordToInsert) != null)
            {
                throw new Exception("Record is already inserted");
            }

            if (!_loadedBlock.IsFull())
            {
                _loadedBlock.Insert(recordToInsert);
                
                WriteBlockToAddress(directoryItem.BlockAddress);
                
                _directory.SetDirectoryItems(hash, blockAddress, directoryItem.Depth, _loadedBlock.ValidRecordsCount);
                
                return;
            }
            
            if (directoryItem.Depth == _directory.Depth)
            {
                _directory.Extend();
            }
            
            // Rozdelenie bloku - Split
            
            // Vytvor nový blok
            var newBlock = new EhfBlock<TRecord>(_blockSize);
            
            // Prerozdelenie zaznamov z plneho bloku do dvoch blokov => prehashovanie
            var records = _loadedBlock.StoredRecords.ToList();
            var blockDepth = directoryItem.Depth + 1;
            
            // Najskor prerozdelenie povodnych zaznamov + ziskame hash zaznamu z oboch blokov
            SplitCurrentRecords(newBlock, records, blockDepth, out var firstItemDirectoryHash, out var secondItemDirectoryHash);
            
            // Pokus o vlozenie aj noveho zaznamu
            if (hash[hash.Length - blockDepth] == false && !_loadedBlock.IsFull())
            {
                _loadedBlock.Insert(recordToInsert);
                inserted = true;
                
                firstItemDirectoryHash = hash;
            }
            else if (hash[hash.Length - blockDepth] == true && !newBlock.IsFull())
            {
                newBlock.Insert(recordToInsert);
                inserted = true;
                
                secondItemDirectoryHash = hash;
            }
            
            long newBlockAddress = -1;
            
            // Kontrola ci sa podarilo vlozit zaznam cize aj prerozdelit zaznamy
            if (inserted)
            {
                // Podarilo sa vlozit zaznam cize sa aj prerozdelili zaznamy => Mozeme zapisat oba bloky na disk
                
                // Zapis povodneho bloku
                WriteBlockToAddress(blockAddress);
                
                // Odlozenie prveho bloku (_loadedBlock [blockAddress])
                var originalBlock = _loadedBlock;
                _loadedBlock = new EhfBlock<TRecord>(_blockSize);
                
                // Vytvorenie a zapis noveho bloku (newBlock [?? -> newBlockAddress])
                newBlockAddress = WriteToFreeBlock(newBlock);
                
                // Obnovenie povodneho bloku
                _loadedBlock = originalBlock;
            }
            else
            {
                // Nepodarilo sa
                
                if (newBlock.IsEmpty())
                {
                    // Vsetky zaznamy ostali v povodnom bloku (az na novy zaznam)
                    
                    // MIESTO ZAPISU ZACACHUJEME PRE DALSIU ITERACIU
                    fullBlockOfRecordsIsAlreadyLoaded = true;
                }
                else
                {
                    // Zamenenie blokov
                    (_loadedBlock, newBlock) = (newBlock, _loadedBlock);
                    
                    // MIESTO ZAPISU ZACACHUJEME PRE DALSIU ITERACIU
                    fullBlockOfRecordsIsAlreadyLoaded = true;
                    
                    // Vsetky zaznamy su v novom bloku (az na novy)
                    newBlockAddress = blockAddress;
                    blockAddress = -1;
                }
            }
            
            // Aktualizacia adresy bloku zduplikovanej polozky
            if (secondItemDirectoryHash != null)
            {
                _directory.SetDirectoryItems(secondItemDirectoryHash, newBlockAddress, blockDepth, newBlock.ValidRecordsCount);
            }
            else
            {
                var tmp = new BitArray(firstItemDirectoryHash);
                tmp[firstItemDirectoryHash.Length - blockDepth] = true;
                _directory.SetDirectoryItems(tmp, newBlockAddress, blockDepth, newBlock.ValidRecordsCount);
            }
            
            // Aktualizacia adresy bloku povodnej polozky
            if (firstItemDirectoryHash != null)
            {
                _directory.SetDirectoryItems(firstItemDirectoryHash, blockAddress, blockDepth, _loadedBlock.ValidRecordsCount);
            }
            else
            {
                secondItemDirectoryHash[secondItemDirectoryHash.Length - blockDepth] = false;
                _directory.SetDirectoryItems(secondItemDirectoryHash, blockAddress, blockDepth, _loadedBlock.ValidRecordsCount);
            }
        }
    }

    private void SplitCurrentRecords(EhfBlock<TRecord> newBlock, List<TRecord> records, int blockDepth, out BitArray? firstItemDirectoryHash, out BitArray? secondItemDirectoryHash)
    {
        firstItemDirectoryHash = null;
        secondItemDirectoryHash = null;
        
        _loadedBlock.Clear();
            
        foreach (var record in records)
        {
            var recordHash = record.GetHash();
                
            // Prerozdelenie podla bitu hlbky bloku
            if (recordHash[recordHash.Length - blockDepth] == false)
            {
                // Povodny blok
                _loadedBlock.Insert(record);
                firstItemDirectoryHash = recordHash;
            }
            else
            {
                newBlock.Insert(record);
                secondItemDirectoryHash = recordHash;
            }
        }
    }
    
    private long WriteFirstBlockToEmptyFile(TRecord recordToInsert)
    {
        // Zapis riadiaceho bloku + noveho prveho bloku
        byte[] bothBlocksBytes = new byte[_blockSize * 2];
        
        // Inicializacia riadiaceho bloku
        _firstFreeBlockAddress = -1;
        _isLoadedControlBlock = true;
        
        // Zaznam z riadiaceho bloku
        BitConverter.GetBytes(_firstFreeBlockAddress).CopyTo(bothBlocksBytes, 0);
        
        var newBlock = new EhfBlock<TRecord>(_blockSize);
        newBlock.Insert(recordToInsert);
        
        newBlock.GetByteArray().CopyTo(bothBlocksBytes, _blockSize);
        
        _fileStream.SetLength(_blockSize * 2);
        
        _fileStream.Seek(0, SeekOrigin.Begin);
        _fileStream.Write(bothBlocksBytes, 0, _blockSize * 2);
        
        _isLoadedControlBlock = true;
        
        return _blockSize;
    }

    private long WriteToFreeBlock(EhfBlock<TRecord> blockWithData)
    {
        if (_firstFreeBlockAddress != -1)
        {
            return WriteToExistingFreeBlock(blockWithData);
        }
        else
        {
            return WriteToNewFreeBlock(blockWithData);
        }
    }

    private long WriteToExistingFreeBlock(EhfBlock<TRecord> blockWithData)
    {
        long blockAddress = _firstFreeBlockAddress;
        
        LoadBlockFromAddress(blockAddress);
        
        // Odobratie z volnych blokov
        var newFirstFreeBlockAddress = _loadedBlock.NextFreeBlockAddress;
        _firstFreeBlockAddress = newFirstFreeBlockAddress;
        
        _loadedBlock = blockWithData;
        
        WriteBlockToAddress(blockAddress);
        
        _loadedBlock = new EhfBlock<TRecord>(_blockSize);
        
        // Oprava referencii prveho volneho bloku
        if (newFirstFreeBlockAddress != -1)
        {
            LoadBlockFromAddress(newFirstFreeBlockAddress);
            _loadedBlock.PreviousFreeBlockAddress = -1;
            WriteBlockToAddress(newFirstFreeBlockAddress);
        }
        
        return blockAddress;
    }
    
    private long WriteToNewFreeBlock(EhfBlock<TRecord> blockWithData)
    {
        long newBlockAddress = ExtendByNewBlock();
        _loadedBlock = blockWithData;
            
        WriteBlockToAddress(newBlockAddress);
        
        return newBlockAddress;
    }
    
    #endregion

    #region DeleteOperations
    
    public void Delete(TRecord recordWithKeyToDelete)
    {
        // Ak sme doteraz nenacitali riadiaci blok, nacitame ho
        if (!_isLoadedControlBlock)
        {
            LoadControlBlock();
        }
        
        var hash = recordWithKeyToDelete.GetHash();
        var directoryItem = _directory.GetDirectoryItem(hash);
        
        if (directoryItem.BlockAddress == -1)
        {
            throw new Exception("Zaznam nebol najdeny");
        }
        
        LoadBlockFromAddress(directoryItem.BlockAddress);
        
        _loadedBlock.Delete(recordWithKeyToDelete);
        
        // Aktualizacia directory => bol upraveny validRecordsCount
        _directory.SetDirectoryItems(hash, directoryItem.BlockAddress, directoryItem.Depth, _loadedBlock.ValidRecordsCount);
        
        while (true)
        {
            directoryItem = _directory.GetDirectoryItem(hash);
            
            // Ak sa uz adresar neda zmensit => koniec
            if (directoryItem.Depth == 0)
            {
                if (_loadedBlock.ValidRecordsCount == 0)
                {
                    DeleteBlock(directoryItem.BlockAddress);
                    _directory.SetDirectoryItems(hash, -1, 0, -1);
                }
                else
                {
                    WriteBlockToAddress(directoryItem.BlockAddress);
                }
                
                return;
            }
            
            // Teraz treba identifikovat, ci vieme zlúčiť susedné bloky ak by sme tym ušetrili jeden voľný blok na dealokáciu
            var neighbourHash = new BitArray(hash);
            neighbourHash[hash.Length - directoryItem.Depth] = !neighbourHash[hash.Length - directoryItem.Depth];
            
            var neighbourDirectoryItem = _directory.GetDirectoryItem(neighbourHash);
            
            if (directoryItem.Depth != neighbourDirectoryItem.Depth)
            {
                // Bloky sa nedaju zlucit => koniec cyklickeho zlucovania
                if (_loadedBlock.ValidRecordsCount == 0)
                {
                    DeleteBlock(directoryItem.BlockAddress);
                    _directory.SetDirectoryItems(hash, -1, directoryItem.Depth, 0);
                }
                else
                {
                    WriteBlockToAddress(directoryItem.BlockAddress);
                }

                return;
            }
            
            // Tato situacia by nikdy namala nastat v prvom behu cyklu ale v dalsich moze a znamena to automaticke zlucenie blokov
            if (neighbourDirectoryItem.BlockAddress == -1)
            {
                if (_loadedBlock.ValidRecordsCount == 0)
                {
                    DeleteBlock(directoryItem.BlockAddress);
                    
                    // Znizime hlbku bloku a zapiseme do adresara
                    _directory.SetDirectoryItems(hash, -1, directoryItem.Depth - 1, 0);
                }
                else
                {
                    // Znizime hlbku bloku a zapiseme do adresara
                    _directory.SetDirectoryItems(hash, directoryItem.BlockAddress, directoryItem.Depth - 1, _loadedBlock.ValidRecordsCount);
                }
                
                // Ak sa da zmensit adresar, tak ho zmensime
                _directory.ShrinkIfPossible();
            }
            else if (_loadedBlock.ValidRecordsCount + neighbourDirectoryItem.ValidRecordsCount <= _loadedBlock.StoredRecords.Length)
            {
                // Zaznamy presuvame zo susedneho bloku do aktualneho bloku
                var currentBlock = _loadedBlock;
                
                _loadedBlock = new EhfBlock<TRecord>(_blockSize);
                LoadBlockFromAddress(neighbourDirectoryItem.BlockAddress);
                var recordsFromNeighbour = _loadedBlock.StoredRecords.Take(neighbourDirectoryItem.ValidRecordsCount).ToList();
                
                _loadedBlock.Clear();
                DeleteBlock(neighbourDirectoryItem.BlockAddress);
                
                foreach (var record in recordsFromNeighbour)
                {
                    currentBlock.Insert(record);
                }
                
                _loadedBlock = currentBlock;
                
                // Znizime hlbku bloku, aktualizujeme pocet zaznamov v bloku a zapiseme do adresara
                _directory.SetDirectoryItems(hash, directoryItem.BlockAddress, directoryItem.Depth - 1, currentBlock.ValidRecordsCount);
                
                // Ak sa da zmensit adresar, tak ho zmensime
                _directory.ShrinkIfPossible();
            }
            else
            {
                // Bloky sa nedaju zlucit => koniec cyklickeho zlucovania
                WriteBlockToAddress(directoryItem.BlockAddress);
                
                return;
            }
        }
    }

    private void DeleteBlock(long currentBlockAddress)
    {
        // Situacie:
        // 1. Blok je na konci suboru
        //    - treba zmenšiť veľkosť suboru + rekurzívne skontrolovať predchádzajúci blok
        // 2. Blok nie je na konci suboru
        //    - zapisať zmenený blok na pôvodnú pozíciu
        //    - treba nastaviť _firstFreeBlockAddress na tento blok a napojiť ho na pôvodný _firstFreeBlockAddress

        // Kontrola ci je blok na konci suboru
        if (currentBlockAddress + _blockSize == _fileStream.Length)
        {
            DeleteBlockAtEnd(currentBlockAddress);
        }
        else
        {
            DeleteBlockAtMiddle(currentBlockAddress);
        }
    }

    private void DeleteBlockAtEnd(long blockAddress)
    {
        ShrinkByBlock();
        
        // Rekurzivne mazanie pripadnych volnych blokov na konci suboru
        while (_firstFreeBlockAddress != -1)
        {
            // Nacitanie posledneho bloku
            LoadBlockFromAddress(blockAddress - _blockSize);
            
            if (!_loadedBlock.IsEmpty())
            {
                return;
            }
            
            ExcludeCurrentBlockFromFree(blockAddress - _blockSize);
            ShrinkByBlock();
            
            blockAddress -= _blockSize;
        }
    }
    
    private void DeleteBlockAtMiddle(long blockAddress)
    {
        // Pridanie do volnych blokov
        var secondFreeBlockAddress = _firstFreeBlockAddress;
        
        _loadedBlock.PreviousFreeBlockAddress = -1;
        _loadedBlock.NextFreeBlockAddress = secondFreeBlockAddress;
        
        _firstFreeBlockAddress = blockAddress;
        
        WriteBlockToAddress(blockAddress);
        
        // Oprava referencii druheho volneho bloku
        if (secondFreeBlockAddress != -1)
        {
            LoadBlockFromAddress(secondFreeBlockAddress);
            _loadedBlock.PreviousFreeBlockAddress = blockAddress;
            WriteBlockToAddress(secondFreeBlockAddress);
        }
    }
    
    private void ExcludeCurrentBlockFromFree(long blockAddress)
    {
        var previousFreeBlockAddress = _loadedBlock.PreviousFreeBlockAddress;
        var nextFreeBlockAddress = _loadedBlock.NextFreeBlockAddress;
        
        // Kontrola ci treba aktualizovat riadicu cast suboru
        if (blockAddress == _firstFreeBlockAddress)
        {
            _firstFreeBlockAddress = nextFreeBlockAddress;
        }
        
        Connect(previousFreeBlockAddress, nextFreeBlockAddress);
    }
    
    private void Connect(long leftBlockAddress, long rightBlockAddress)
    {
        if (leftBlockAddress != -1)
        {
            LoadBlockFromAddress(leftBlockAddress);
            _loadedBlock.NextFreeBlockAddress = rightBlockAddress;
            WriteBlockToAddress(leftBlockAddress);
        }
        
        if (rightBlockAddress != -1)
        {
            LoadBlockFromAddress(rightBlockAddress);
            _loadedBlock.PreviousFreeBlockAddress = leftBlockAddress;
            WriteBlockToAddress(rightBlockAddress);
        }
    }
    
    #endregion

    #region DirectoryOperations

    private EhfDirectory LoadDirectory()
    {
        var directoryFileName = GetDirectoryFileName();
        
        _fileStream.Seek(0, SeekOrigin.Begin);
        if (_fileStream.Length == 0)
        {
            return new EhfDirectory(directoryFileName);
        }
        
        return EhfDirectory.LoadFromFile(directoryFileName);
    }
    
    
    public string GetDirectoryFileName()
    {
        var indexOfLastDot = _fileStream.Name.LastIndexOf('.');
        if (indexOfLastDot != -1)
        {
            var nameOfFileWithoutExtension = _fileStream.Name.Substring(0, indexOfLastDot);
            var extension = _fileStream.Name.Substring(indexOfLastDot);
            return nameOfFileWithoutExtension + ".dir" + extension;
        }
        
        return _fileStream.Name + ".dir";
    }

    #endregion
    
    #region BlocksOperations
    
    protected void LoadBlockFromAddress(long blockAddress)
    {
        var blockBytes = new byte[_blockSize];
        
        _fileStream.Seek(blockAddress, SeekOrigin.Begin);
        _fileStream.Read(blockBytes, 0, _blockSize);
        
        _loadedBlock.FromByteArray(blockBytes);
    }
    
    protected void WriteBlockToAddress(long blockAddress)
    {
        _fileStream.Seek(blockAddress, SeekOrigin.Begin);
        _fileStream.Write(_loadedBlock.GetByteArray(), 0, _loadedBlock.GetBytesSize());
    }

    #endregion

    #region ControlBlockOperations
    
    private void WriteControlBlock()
    {
        byte[] controlBlockBytes = new byte[_blockSize];
        
        BitConverter.GetBytes(_firstFreeBlockAddress).CopyTo(controlBlockBytes, 0);
        
        _fileStream.Seek(0, SeekOrigin.Begin);
        _fileStream.Write(controlBlockBytes, 0, _blockSize);
        
        _isLoadedControlBlock = true;
    }
    
    private void LoadControlBlock()
    {
        byte[] controlBlockBytes = new byte[_blockSize];
        
        _fileStream.Seek(0, SeekOrigin.Begin);
        _fileStream.Read(controlBlockBytes, 0, _blockSize);
        
        _firstFreeBlockAddress = BitConverter.ToInt64(controlBlockBytes[0..sizeof(long)]);
        
        _isLoadedControlBlock = true;
    }
    
    #endregion

    #region FileSizeOperations

    private long ExtendByNewBlock()
    {
        // Ak je subor prazdny, treba vyhradit miesto pre riadiaci blok (prvych _blockSize bytov)
        // a az potom pre prvy blok (druhych _blockSize bytov)
        if (_fileStream.Length == 0)
        {
            _fileStream.SetLength(_blockSize + _blockSize);
            return _blockSize;
        }
        
        _fileStream.SetLength(_fileStream.Length + _blockSize);
        
        // Adresa noveho bloku
        return _fileStream.Length - _blockSize;
    }
    
    private void ShrinkByBlock()
    {
        _fileStream.SetLength(_fileStream.Length - _blockSize);
    }

    #endregion

    #region Debug

    public EhfDebug<TRecord> GetDebugObject()
    {
        if (!_isLoadedControlBlock)
        {
            LoadControlBlock();
        }
        
        var blocks = new List<EhfBlockDebug<TRecord>>();
        
        for (var i = _blockSize; i < _fileStream.Length; i += _blockSize)
        {
            var block = new EhfBlock<TRecord>(_blockSize);
            
            _fileStream.Seek(i, SeekOrigin.Begin);
            var blockBytes = new byte[_blockSize];
            _fileStream.Read(blockBytes, 0, _blockSize);
            
            block.FromByteArray(blockBytes);
            blocks.Add(new EhfBlockDebug<TRecord>
            {
                BlockAddress = i,
                ValidRecordsCount = block.ValidRecordsCount,
                NextFreeBlockAddress = block.NextFreeBlockAddress,
                PreviousFreeBlockAddress = block.PreviousFreeBlockAddress,
                StoredRecords = block.StoredRecords.ToList()
            });
        }
        
        return new EhfDebug<TRecord>()
        {
            FirstFreeBlockAddress = _firstFreeBlockAddress,
            DirectoryDepth = _directory.Depth,
            Directory = _directory.GetDirectoryCopy(),
            Blocks = blocks
        };
    }

    #endregion
}
