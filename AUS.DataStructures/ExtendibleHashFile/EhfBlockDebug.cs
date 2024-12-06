namespace AUS.DataStructures.ExtendibleHashFile;

public class EhfBlockDebug<TRecord>
{
    public long BlockAddress { get; set; }
    
    public string BlockAddressLabel => $"[{BlockAddress}]";
    
    public int ValidRecordsCount { get; set; }
    
    public long NextFreeBlockAddress { get; set; }
    
    public long PreviousFreeBlockAddress { get; set; }
    
    public List<TRecord> StoredRecords { get; set; } = new();
}
