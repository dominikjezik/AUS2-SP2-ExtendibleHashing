namespace AUS.DataStructures.ExtendibleHashFile;

public class EhfDirectoryItem
{
    public long BlockAddress { get; private set; }
    
    public int Depth { get; private set; }
    
    public int ValidRecordsCount { get; private set; }
    
    public EhfDirectoryItem(long blockAddress, int depth, int validRecordsCount)
    {
        BlockAddress = blockAddress;
        Depth = depth;
        ValidRecordsCount = validRecordsCount;
    }
    
    public int DebugIndex { get; set; }
}
