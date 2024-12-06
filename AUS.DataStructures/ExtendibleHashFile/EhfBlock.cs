namespace AUS.DataStructures.ExtendibleHashFile;

public class EhfBlock<TRecord> where TRecord : IEhfRecord, new()
{
    private const int ControlPartSize = sizeof(long) * 2 + sizeof(int);

    public int ValidRecordsCount { get; private set; } = 0;
    
    public long NextFreeBlockAddress { get; set; } = -1;

    public long PreviousFreeBlockAddress { get; set; } = -1;

    private readonly int _sizeOfRecord = new TRecord().GetBytesSize();

    public TRecord[] StoredRecords { get; private set; }

    private readonly int _blockSize;

    public EhfBlock(int blockSize)
    {
        _blockSize = blockSize;
        var blockFactor = (blockSize - ControlPartSize) / _sizeOfRecord;
        StoredRecords = new TRecord[blockFactor];
        
        FillStoredRecords();
    }
    
    public void Clear()
    {
        ValidRecordsCount = 0;
        NextFreeBlockAddress = -1;
        PreviousFreeBlockAddress = -1;
        
        FillStoredRecords();
    }
    
    private void FillStoredRecords()
    {
        for (var i = 0; i < StoredRecords.Length; i++)
        {
            StoredRecords[i] = new TRecord();
        }
    }
    
    public int Capacity => StoredRecords.Length;
    
    public bool IsFull()
    {
        return ValidRecordsCount == StoredRecords.Length;
    }
    
    public bool IsEmpty()
    {
        return ValidRecordsCount == 0;
    }
    
    public void Insert(TRecord record)
    {
        if (ValidRecordsCount >= StoredRecords.Length)
        {
            throw new Exception("Block is full");
        }
        
        // Kontrola ci sa uz zaznam nenachadza v bloku
        for (var i = 0; i < ValidRecordsCount; i++)
        {
            if (StoredRecords[i].Equals(record))
            {
                throw new Exception("Record is already inserted");
            }
        }
        
        // Vždy zapisujeme čo najviac doľava
        StoredRecords[ValidRecordsCount] = record;
        ValidRecordsCount++;
    }

    public TRecord? Get(TRecord recordToCompare)
    {
        for (var i = 0; i < ValidRecordsCount; i++)
        {
            var r = StoredRecords[i].Equals(recordToCompare);
            
            if (r)
            {
                return StoredRecords[i];
            }
        }
        
        return default;
    }
    
    public void Delete(TRecord recordToCompare)
    {
        for (var i = 0; i < ValidRecordsCount; i++)
        {
            if (StoredRecords[i].Equals(recordToCompare))
            {
                // Vymen s poslednym validnym zaznamom
                StoredRecords[i] = StoredRecords[ValidRecordsCount - 1];
                
                // Vymaz posledny validny zaznam
                StoredRecords[ValidRecordsCount - 1] = new TRecord();
                
                ValidRecordsCount--;
                
                return;
            }
        }
        
        throw new Exception("Record not found");
    }
    
    public int GetBytesSize()
    {
        return _blockSize;
    }

    public byte[] GetByteArray()
    {
        var byteArray = new byte[_blockSize];
        
        // Convert riadiacej časti
        var nextFreeBlockAddressBytes = BitConverter.GetBytes(NextFreeBlockAddress);
        var previousFreeBlockAddressBytes = BitConverter.GetBytes(PreviousFreeBlockAddress);
        var validRecordsCountBytes = BitConverter.GetBytes(ValidRecordsCount);
        
        Array.Copy(nextFreeBlockAddressBytes, 0, byteArray, 0, nextFreeBlockAddressBytes.Length);
        
        Array.Copy(previousFreeBlockAddressBytes, 0, byteArray, nextFreeBlockAddressBytes.Length, previousFreeBlockAddressBytes.Length);
        
        Array.Copy(validRecordsCountBytes, 0, byteArray, nextFreeBlockAddressBytes.Length + previousFreeBlockAddressBytes.Length, validRecordsCountBytes.Length);
        
        
        // Convert záznamov
        for (var i = 0; i < StoredRecords.Length; i++)
        {
            var recordBytes = StoredRecords[i].GetByteArray();
            Array.Copy(recordBytes, 0, byteArray, ControlPartSize + i * _sizeOfRecord, recordBytes.Length);
        }
        
        // Doplnenie prázdneho miesta na veľkosť bloku
        for (var i = ControlPartSize + StoredRecords.Length * _sizeOfRecord; i < _blockSize; i++)
        {
            byteArray[i] = 0;
        }
        
        return byteArray;
    }

    public void FromByteArray(byte[] byteArray)
    {
        Clear();
        
        // Convert riadiacej časti
        var nextFreeBlockAddressBytes = new byte[sizeof(long)];
        var previousFreeBlockAddressBytes = new byte[sizeof(long)];
        var validRecordsCountBytes = new byte[sizeof(int)];
        
        Array.Copy(byteArray, 0, nextFreeBlockAddressBytes, 0, nextFreeBlockAddressBytes.Length);
        Array.Copy(byteArray, nextFreeBlockAddressBytes.Length, previousFreeBlockAddressBytes, 0, previousFreeBlockAddressBytes.Length);
        Array.Copy(byteArray, nextFreeBlockAddressBytes.Length + previousFreeBlockAddressBytes.Length, validRecordsCountBytes, 0, validRecordsCountBytes.Length);
        
        NextFreeBlockAddress = BitConverter.ToInt64(nextFreeBlockAddressBytes);
        PreviousFreeBlockAddress = BitConverter.ToInt64(previousFreeBlockAddressBytes);
        ValidRecordsCount = BitConverter.ToInt32(validRecordsCountBytes);
        
        // Convert záznamov
        for (var i = 0; i < StoredRecords.Length; i++)
        {
            var recordBytes = new byte[_sizeOfRecord];
            Array.Copy(byteArray, ControlPartSize + i * _sizeOfRecord, recordBytes, 0, recordBytes.Length);
            
            StoredRecords[i].FromByteArray(recordBytes);
        }
    }
}
