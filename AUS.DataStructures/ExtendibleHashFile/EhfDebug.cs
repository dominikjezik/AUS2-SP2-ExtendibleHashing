namespace AUS.DataStructures.ExtendibleHashFile;

public class EhfDebug<TRecord> where TRecord : IEhfRecord, new()
{
    public long FirstFreeBlockAddress { get; set; }
    
    public int DirectoryDepth { get; set; }
    
    public EhfDirectoryItem[] Directory { get; set; } = new EhfDirectoryItem[1];
    
    public List<EhfBlockDebug<TRecord>> Blocks { get; set; } = new();
}
