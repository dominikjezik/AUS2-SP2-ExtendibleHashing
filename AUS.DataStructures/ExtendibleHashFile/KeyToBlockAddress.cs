using System.Collections;

namespace AUS.DataStructures.ExtendibleHashFile;

public class KeyToBlockAddress<TKey> : IEhfRecord where TKey : IEhfRecord, new()
{
    public TKey Key { get; set; } = new();
    
    public long BlockAddress { get; set; }


    public int GetBytesSize()
    {
        return Key.GetBytesSize() + sizeof(long);
    }

    public byte[] GetByteArray()
    {
        var byteArray = new byte[GetBytesSize()];
        
        var keyBytes = Key.GetByteArray();
        var blockAddressBytes = BitConverter.GetBytes(BlockAddress);
        
        keyBytes.CopyTo(byteArray, 0);
        blockAddressBytes.CopyTo(byteArray, keyBytes.Length);
        
        return byteArray;
    }

    public void FromByteArray(byte[] byteArray)
    {
        var keyBytes = new byte[Key.GetBytesSize()];
        var blockAddressBytes = new byte[sizeof(long)];
        
        Array.Copy(byteArray, 0, keyBytes, 0, keyBytes.Length);
        Array.Copy(byteArray, keyBytes.Length, blockAddressBytes, 0, blockAddressBytes.Length);
        
        Key.FromByteArray(keyBytes);
        BlockAddress = BitConverter.ToInt64(blockAddressBytes, 0);
    }
    
    public bool Equals(IEhfRecord? other)
    {
        return other is KeyToBlockAddress<TKey> keyToBlockAddress &&
               Key.Equals(keyToBlockAddress.Key);
    }

    public BitArray GetHash()
    {
        return Key.GetHash();
    }
}
