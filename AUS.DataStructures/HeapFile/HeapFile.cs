namespace AUS.DataStructures.HeapFile;

public class HeapFile<TRecord> where TRecord : IHfRecord, new()
{
    private int _blockSize;
    
    private HfBlock<TRecord> _loadedBlock;
    
    private FileStream _fileStream;
    
    private bool _isLoadedControlBlock = false;
    
    private long _firstFreeBlockAddress = -1;
    
    private long _firstPartiallyFreeBlockAddress = -1;
    
    public HeapFile(string fileName, int blockSize)
    {
        _blockSize = blockSize;
        
        _loadedBlock = new HfBlock<TRecord>(blockSize);
        _fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
    }
    
    public void Close()
    {
        // Ak sa v subore nenachadza ziadny zaznam, odstrani sa riadiaci blok
        if (_fileStream.Length == _blockSize)
        {
            _fileStream.SetLength(0);
            _fileStream.Close();
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
    }
    
    public long Insert(TRecord record)
    {
        // Ak v subore nie je nic, treba inicializovat riadiaci blok (zatedy v operacnej pamati)
        if (_fileStream.Length == 0)
        {
            _firstFreeBlockAddress = -1;
            _firstPartiallyFreeBlockAddress = -1;
            
            _isLoadedControlBlock = true;
        }
        
        // Ak sme doteraz nenacitali riadiaci blok, nacitame ho
        if (!_isLoadedControlBlock)
        {
            LoadControlBlock();
        }
        
        // Kontrola ci mam nejaky ciastocne volny blok
        if (_firstPartiallyFreeBlockAddress != -1)
        {
            return InsertIntoPartiallyFreeBlock(record);
        }
        
        // Kontrola ci mam nejaky volny blok
        if (_firstFreeBlockAddress != -1)
        {
            return InsertIntoExistingFreeBlock(record);
        }

        return InsertIntoNewFreeBlock(record);
    }

    private long InsertIntoPartiallyFreeBlock(TRecord record)
    {
        long blockAddress = _firstPartiallyFreeBlockAddress;
        LoadBlockFromAddress(blockAddress);
        
        _loadedBlock.Insert(record);
        
        if (!_loadedBlock.IsFull())
        {
            WriteBlockToAddress(blockAddress);
            return blockAddress;
        }
        
        // Blok je plny => odobratie z ciastocne volnych blokov
        var newFirstPartiallyFreeBlockAddress = _loadedBlock.NextFreeBlockAddress;
        
        // Odpojenie bloku
        _loadedBlock.NextFreeBlockAddress = -1;
        
        WriteBlockToAddress(blockAddress);
        
        // Aktualizacia riadiacej casti na novy prvy ciastocne volny blok
        _firstPartiallyFreeBlockAddress = newFirstPartiallyFreeBlockAddress;
        
        // Aktualizacia noveho prveho ciastocne volneho bloku
        if (newFirstPartiallyFreeBlockAddress != -1)
        {
            LoadBlockFromAddress(newFirstPartiallyFreeBlockAddress);
            _loadedBlock.PreviousFreeBlockAddress = -1;
            WriteBlockToAddress(newFirstPartiallyFreeBlockAddress);
        }
        
        return blockAddress;
    }

    private long InsertIntoExistingFreeBlock(TRecord record)
    {
        long blockAddress = _firstFreeBlockAddress;
        LoadBlockFromAddress(blockAddress);
        
        _loadedBlock.Insert(record);
        
        // Odobratie z volnych blokov
        var newFirstFreeBlockAddress = _loadedBlock.NextFreeBlockAddress;
        
        _firstFreeBlockAddress = newFirstFreeBlockAddress;
        
        _loadedBlock.NextFreeBlockAddress = -1;
        _loadedBlock.PreviousFreeBlockAddress = -1;
        
        if (_loadedBlock.Capacity != 1)
        {
            // Zaradenie do ciastocne volnych blokov => teraz tam bude jediny blok
            _firstPartiallyFreeBlockAddress = blockAddress;
        }
        
        WriteBlockToAddress(blockAddress);
        
        // Oprava referencii prveho volneho bloku
        if (newFirstFreeBlockAddress != -1)
        {
            LoadBlockFromAddress(newFirstFreeBlockAddress);
            _loadedBlock.PreviousFreeBlockAddress = -1;
            WriteBlockToAddress(newFirstFreeBlockAddress);
        }
        
        return blockAddress;
    }
    
    private long InsertIntoNewFreeBlock(TRecord record)
    {
        long blockAddress = ExtendByNewBlock();
        
        // Nastavenie instancie bloku na novy blok
        _loadedBlock.Clear();
        
        // Vlozenie zaznamu
        _loadedBlock.Insert(record);
        
        // Zapisanie bloku s novym zaznamom
        WriteBlockToAddress(blockAddress);
        
        // Poznacit tento novy blok do ciastocne volnych blokov (mohol by byt plny ak je miesto iba na jeden zaznam)
        if (!_loadedBlock.IsFull())
        {
            _firstPartiallyFreeBlockAddress = blockAddress;
        }
        
        return blockAddress;
    }
    
    public TRecord? Get(long blockAddress, TRecord recordToCompare)
    {
        LoadBlockFromAddress(blockAddress);
        return _loadedBlock.Get(recordToCompare);
    }
    
    public void Update(long blockAddress, TRecord recordToCompare, TRecord recordToUpdate)
    {
        LoadBlockFromAddress(blockAddress);
        _loadedBlock.Update(recordToCompare, recordToUpdate);
        WriteBlockToAddress(blockAddress);
    }
    
    public void Delete(long blockAddress, TRecord recordToCompare)
    {
        if (_fileStream.Length == 0)
        {
            throw new Exception("Record not found");
        }
        
        // Ak sme doteraz nenacitali riadiaci blok, nacitame ho
        if (!_isLoadedControlBlock)
        {
            LoadControlBlock();
        }
        
        LoadBlockFromAddress(blockAddress);
        
        var numberOfRecordsBeforeDelete = _loadedBlock.ValidRecordsCount;

        // Vymazanie zaznamu a vnútorné striasenie bloku
        _loadedBlock.Delete(recordToCompare);
        
        // Situacie:
        // 1. Blok je prazdny a je na konci suboru
        //    - treba zmenšiť veľkosť suboru + rekurzívne skontrolovať predchádzajúci blok
        // 2. Blok je prazdny a nie je na konci suboru
        //    - zapisať zmenený blok na pôvodnú pozíciu
        //    - treba nastaviť _firstFreeBlockAddress na tento blok a napojiť ho na pôvodný _firstFreeBlockAddress
        // 3. Blok nie je prazdny ale uz bol v ciastocne volnych blokoch (pred zmazanim nebol plny)
        //    - iba aktualizovat blok
        // 4. Blok nie je prazdny a nebol v ciastocne volnych blokoch (pred zmazanim bol plny)
        //    - zapisať zmenený blok na pôvodnú pozíciu
        //    - treba nastaviť _firstPartiallyFreeBlockAddress na tento blok a napojiť ho na pôvodný _firstPartiallyFreeBlockAddress

        if (_loadedBlock.IsEmpty())
        {
            // Kontrola ci je blok na konci suboru
            if (blockAddress + _blockSize == _fileStream.Length)
            {
                DeleteBlockAtEnd(blockAddress);
            }
            else
            {
                if (_loadedBlock.Capacity == 1)
                {
                    DeleteBlockAtMiddleWithCapacity1(blockAddress);
                }
                else
                {
                    DeleteBlockAtMiddle(blockAddress);
                }
            }
            
            return;
        }
        
        // Blok nebol pred zmazanim plny => uz sa nachadza v ciastocne volnych blokoch
        if (numberOfRecordsBeforeDelete < _loadedBlock.Capacity)
        {
            WriteBlockToAddress(blockAddress);
            return;
        }
        
        
        // Blok bol pred zmazanim plny (nenachadzal sa v ciastocne volnych blokoch)
        var secondPartiallyFreeBlockAddress = _firstPartiallyFreeBlockAddress;
        
        // Prepojenie bloku na nasledujuci ciasnocne volny blok (tento bol povodne prvy volny)
        _loadedBlock.NextFreeBlockAddress = secondPartiallyFreeBlockAddress;
        // Teoreticky -1 nastaviť ale prakticky by to nemalo nastať že je iné ako -1
        //_loadedBlock.PreviousFreeBlockAddress = -1;
                
        // Nastavenie bloku ako prvy volny
        _firstPartiallyFreeBlockAddress = blockAddress;
                
        // Zapisanie bloku
        WriteBlockToAddress(blockAddress);
        
        // Napojenie druhého bloku na tento nový/prvý (ciastocne volne bloky)
        if (secondPartiallyFreeBlockAddress != -1)
        {
            LoadBlockFromAddress(secondPartiallyFreeBlockAddress);
            _loadedBlock.PreviousFreeBlockAddress = blockAddress;
            WriteBlockToAddress(secondPartiallyFreeBlockAddress);
        }
    }

    private void DeleteBlockAtEnd(long blockAddress)
    {
        // Ak je blokovaci faktor 1 neexistuju ciastocne volne bloky
        if (_loadedBlock.Capacity != 1)
        {
            ExcludeCurrentBlockFromPartiallyFree(blockAddress);
        }

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

    private void ExcludeCurrentBlockFromPartiallyFree(long blockAddress)
    {
        var previousFreeBlockAddress = _loadedBlock.PreviousFreeBlockAddress;
        var nextFreeBlockAddress = _loadedBlock.NextFreeBlockAddress;
        
        // Kontrola ci treba aktualizovat riadicu cast suboru
        if (blockAddress == _firstPartiallyFreeBlockAddress)
        {
            _firstPartiallyFreeBlockAddress = nextFreeBlockAddress;
        }
        
        Connect(previousFreeBlockAddress, nextFreeBlockAddress);
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
    
    private void DeleteBlockAtMiddle(long blockAddress)
    {
        // Odobranie z ciastocne volnych blokov a zapojenie do volnych blokov
                
        // Poznacenie povodnych referencii v ciastocne volnych blokoch
        var previousPartiallyFreeBlockAddress = _loadedBlock.PreviousFreeBlockAddress;
        var nextPartiallyFreeBlockAddress = _loadedBlock.NextFreeBlockAddress;
        
        // Pridanie do volnych blokov
        var secondFreeBlockAddress = _firstFreeBlockAddress;
        
        // previous treba nastavit na -1, lebo mohol ukazovat na iny ciastocne volny blok
        _loadedBlock.PreviousFreeBlockAddress = -1;
        _loadedBlock.NextFreeBlockAddress = secondFreeBlockAddress;
        
        _firstFreeBlockAddress = blockAddress;
        
        WriteBlockToAddress(blockAddress);
                
        // Odobratie z ciastocne volnych blokov

        // Kontrola ci treba aktualizovat riadicu cast suboru
        if (blockAddress == _firstPartiallyFreeBlockAddress)
        {
            _firstPartiallyFreeBlockAddress = nextPartiallyFreeBlockAddress;
        }
        
        Connect(previousPartiallyFreeBlockAddress, nextPartiallyFreeBlockAddress);
        
        // Oprava referencii druheho volneho bloku
        if (secondFreeBlockAddress != -1)
        {
            LoadBlockFromAddress(secondFreeBlockAddress);
            _loadedBlock.PreviousFreeBlockAddress = blockAddress;
            WriteBlockToAddress(secondFreeBlockAddress);
        }
    }

    private void DeleteBlockAtMiddleWithCapacity1(long blockAddress)
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
        BitConverter.GetBytes(_firstPartiallyFreeBlockAddress).CopyTo(controlBlockBytes, sizeof(long));
        
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
        _firstPartiallyFreeBlockAddress = BitConverter.ToInt64(controlBlockBytes[sizeof(long)..(2 * sizeof(long))]);
        
        _isLoadedControlBlock = true;
    }
    
    #endregion

    #region FileSizeOperations

    private long ExtendByNewBlock()
    {
        // Ak je subor prazdny, treba vyhradit miesto pre riadiaci blok (prvych _blockSize bytov)
        // a az potom pre prvy blok (druchych _blockSize bytov)
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

    public HfDebug <TRecord> GetDebugObject()
    {
        if (!_isLoadedControlBlock)
        {
            LoadControlBlock();
        }
        
        var blocks = new List<HfBlockDebug<TRecord>>();
        
        for (var i = _blockSize; i < _fileStream.Length; i += _blockSize)
        {
            var block = new HfBlock<TRecord>(_blockSize);
            
            _fileStream.Seek(i, SeekOrigin.Begin);
            var blockBytes = new byte[_blockSize];
            _fileStream.Read(blockBytes, 0, _blockSize);
            
            block.FromByteArray(blockBytes);
            blocks.Add(new HfBlockDebug<TRecord>
            {
                BlockAddress = i,
                ValidRecordsCount = block.ValidRecordsCount,
                NextFreeBlockAddress = block.NextFreeBlockAddress,
                PreviousFreeBlockAddress = block.PreviousFreeBlockAddress,
                StoredRecords = block.StoredRecords.ToList()
            });
        }
        
        return new HfDebug<TRecord>()
        {
            FirstFreeBlockAddress = _firstFreeBlockAddress,
            FirstPartiallyFreeBlockAddress = _firstPartiallyFreeBlockAddress,
            Blocks = blocks
        };
    }
    
    public HfBlockDebug<TRecord> GetLastBlockForDebug()
    {
        var lastBlockAddress = _fileStream.Length - _blockSize;
        
        var block = new HfBlock<TRecord>(_blockSize);
        
        _fileStream.Seek(lastBlockAddress, SeekOrigin.Begin);
        var blockBytes = new byte[_blockSize];
        _fileStream.Read(blockBytes, 0, _blockSize);
        
        block.FromByteArray(blockBytes);
        
        return new HfBlockDebug<TRecord>
        {
            BlockAddress = lastBlockAddress,
            ValidRecordsCount = block.ValidRecordsCount,
            NextFreeBlockAddress = block.NextFreeBlockAddress,
            PreviousFreeBlockAddress = block.PreviousFreeBlockAddress,
            StoredRecords = block.StoredRecords.ToList()
        };
    }

    #endregion
}
