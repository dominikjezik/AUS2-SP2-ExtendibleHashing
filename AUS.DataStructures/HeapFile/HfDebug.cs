namespace AUS.DataStructures.HeapFile;

public class HfDebug<TRecord> where TRecord : IHfRecord, new()
{
    public long FirstFreeBlockAddress { get; set; }
    
    public long FirstPartiallyFreeBlockAddress { get; set; }
    
    public List<HfBlockDebug<TRecord>> Blocks { get; set; } = new();
}
