using System.Collections;

namespace AUS.DataStructures.ExtendibleHashFile;

public class EhfDirectory
{
    private FileStream _fileStream;
    private EhfDirectoryItem[] _directoryItems;

    public int Depth { get; private set; } = 0;
    
    public EhfDirectory(string fileName)
    {
        _directoryItems = [ new EhfDirectoryItem(-1, 0, -1) ];
        _fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
    }
    
    public EhfDirectory(int depth, EhfDirectoryItem[] directoryItems, FileStream fileStream)
    {
        Depth = depth;
        _directoryItems = directoryItems;
        _fileStream = fileStream;
    }
    
    public long GetAddress(BitArray hash)
    {
        var index = GetIndex(hash);
        return _directoryItems[index].BlockAddress;
    }
    
    public EhfDirectoryItem GetDirectoryItem(BitArray hash)
    {
        var index = GetIndex(hash);
        return _directoryItems[index];
    }

    public void SetDirectoryItems(BitArray hash, long address, int blockDepth, int validRecordsCount)
    {
        // Adresu treba nastaviť pre všetky také indexy kde sa zhoduje prvých depth-bitov zo zadaného hashu
        // Nech depth = 2, hash = 1010100, potom treba nastaviť adresu pre všetky indexy 1000000-1011111
        if (blockDepth == 0)
        {
            _directoryItems = [ new EhfDirectoryItem(address, blockDepth, validRecordsCount) ];
            Depth = 0;
            
            return;
        }
        
        // Zober zo startovacieho hashu prvé depth bitov zlava a ostatne nech su 0
        var startHash = new BitArray(hash);
        var endHash = new BitArray(hash);
        for (int i = blockDepth; i < startHash.Length; i++)
        {
            startHash[startHash.Length - 1 - i] = false;
            endHash[startHash.Length - 1 - i] = true;
        }
        
        // Preved startHash a endHash na cele cislo
        var startHashIndex = 0;
        var endHashIndex = 0;
        for (int i = 0; i < Depth; i++)
        {
            
            
            if (startHash[startHash.Length - 1 - i])
            {
                //startHashIndex += (int)Math.Pow(2, Depth - i - 1);
                startHashIndex |= 1 << (Depth - i - 1);
            }
            
            if (endHash[endHash.Length - 1 - i])
            {
                //endHashIndex += (int)Math.Pow(2, Depth - i - 1);
                endHashIndex |= 1 << (Depth - i - 1);
            }
        }
        
        for (int i = startHashIndex; i <= endHashIndex; i++)
        {
            _directoryItems[i] = new EhfDirectoryItem(address, blockDepth, validRecordsCount);
        }
    }
    
    private int GetIndex(BitArray hash)
    {
        var index = 0;
        
        for (int i = 0; i < Depth; i++)
        {
            if (hash[hash.Length - 1 - i])
            {
                //index += (int)Math.Pow(2, Depth - i - 1);
                // ekvivalent
                index |= 1 << (Depth - i - 1);
            }
        }
        
        return index;
    }
    
    public void Extend()
    {
        var newDirectory = new EhfDirectoryItem[_directoryItems.Length * 2];
        for (int i = 0; i < _directoryItems.Length; i++)
        {
            newDirectory[2 * i] = _directoryItems[i];
            newDirectory[2 * i + 1] = new EhfDirectoryItem(_directoryItems[i].BlockAddress, _directoryItems[i].Depth, _directoryItems[i].ValidRecordsCount);
        }
        
        _directoryItems = newDirectory;
        Depth++;
    }
    
    public void ShrinkIfPossible()
    {
        while (CanShrink())
        {
            Shrink();   
        }
    }
    
    public bool CanShrink()
    {
        if (Depth == 0)
        {
            return false;
        }
        
        for (int i = 0; i < _directoryItems.Length; i += 2)
        {
            if (_directoryItems[i].BlockAddress != _directoryItems[i + 1].BlockAddress)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void Shrink()
    {
        var newDirectory = new EhfDirectoryItem[_directoryItems.Length / 2];
        for (int i = 0; i < newDirectory.Length; i++)
        {
            newDirectory[i] = _directoryItems[2 * i];
        }
        
        _directoryItems = newDirectory;
        Depth--;
    }
    
    public static EhfDirectory LoadFromFile(string fileName)
    {
        var fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        var bytes = new byte[fileStream.Length];
        
        fileStream.Read(bytes, 0, bytes.Length);
        
        var depth = BitConverter.ToInt32(bytes[..sizeof(int)]);
        var numberOfItems = (int)(Math.Pow(2, depth));
        
        var directoryItems = new EhfDirectoryItem[numberOfItems];
        
        var sizeOfOneItem = sizeof(long) + sizeof(int) + sizeof(int);
        
        for (int i = 0; i < numberOfItems; i++)
        {
            var blockAddress = BitConverter.ToInt64(bytes[(sizeof(int) + i * sizeOfOneItem)..(sizeof(int) + i * sizeOfOneItem + sizeof(long))]);
            var depthItem = BitConverter.ToInt32(bytes[(sizeof(int) + i * sizeOfOneItem + sizeof(long))..(sizeof(int) + i * sizeOfOneItem + sizeof(long) + sizeof(int))]);
            var validRecordsCount = BitConverter.ToInt32(bytes[(sizeof(int) + i * sizeOfOneItem + sizeof(long) + sizeof(int))..(sizeof(int) + i * sizeOfOneItem + sizeof(long) + sizeof(int) + sizeof(int))]);
            
            directoryItems[i] = new EhfDirectoryItem(blockAddress, depthItem, validRecordsCount);
        }
        
        return new EhfDirectory(depth, directoryItems, fileStream);
    }

    public void SaveToFile()
    {
        var bytes = new byte[sizeof(int) + _directoryItems.Length * (sizeof(long) + sizeof(int) + sizeof(int))];
        
        BitConverter.GetBytes(Depth).CopyTo(bytes, 0);
        
        for (int i = 0; i < _directoryItems.Length; i++)
        {
            BitConverter.GetBytes(_directoryItems[i].BlockAddress).CopyTo(bytes, sizeof(int) + i * (sizeof(long) + sizeof(int) + sizeof(int)));
            BitConverter.GetBytes(_directoryItems[i].Depth).CopyTo(bytes, sizeof(int) + i * (sizeof(long) + sizeof(int) + sizeof(int)) + sizeof(long));
            BitConverter.GetBytes(_directoryItems[i].ValidRecordsCount).CopyTo(bytes, sizeof(int) + i * (sizeof(long) + sizeof(int) + sizeof(int)) + sizeof(long) + sizeof(int));
        }
        
        _fileStream.SetLength(bytes.Length);
        _fileStream.Seek(0, SeekOrigin.Begin);
        _fileStream.Write(bytes, 0, bytes.Length);
        
        _fileStream.Flush();
        _fileStream.Close();
    }

    public void DeleteFile()
    {
        _fileStream.Close();
        File.Delete(_fileStream.Name);
    }
    
    public EhfDirectoryItem[] GetDirectoryCopy()
    {
        var copy = new EhfDirectoryItem[_directoryItems.Length];
        for (int i = 0; i < _directoryItems.Length; i++)
        {
            copy[i] = new EhfDirectoryItem(_directoryItems[i].BlockAddress, _directoryItems[i].Depth, _directoryItems[i].ValidRecordsCount);
        }
        return copy;
    }
}
