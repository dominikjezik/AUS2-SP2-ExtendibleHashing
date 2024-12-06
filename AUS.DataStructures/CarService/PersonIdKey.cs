using System.Collections;
using AUS.DataStructures.ExtendibleHashFile;

namespace AUS.DataStructures.CarService;

public class PersonIdKey : IEhfRecord
{
    public int Value { get; set; }
    
    public int GetBytesSize() => sizeof(int);

    public byte[] GetByteArray() => BitConverter.GetBytes(Value);

    public void FromByteArray(byte[] byteArray)
    {
        Value = BitConverter.ToInt32(byteArray, 0);
    }

    public bool Equals(IEhfRecord? other)
    {
        return other is PersonIdKey personIdKey &&
               Value == personIdKey.Value;
    }

    public BitArray GetHash()
    {
        var bitArray = new BitArray(BitConverter.GetBytes(Value));
        var reversed = new BitArray(bitArray.Length);
        
        for (var i = 0; i < bitArray.Length; i++)
        {
            reversed[i] = bitArray[bitArray.Length - i - 1];
        }
        
        return reversed;
    }
}
