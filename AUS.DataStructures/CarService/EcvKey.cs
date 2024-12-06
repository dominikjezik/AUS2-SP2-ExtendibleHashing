using System.Collections;
using System.Text;
using AUS.DataStructures.ExtendibleHashFile;

namespace AUS.DataStructures.CarService;

public class EcvKey : IEhfRecord
{
    private const byte MaxLength = 10;
    
    private string _value = string.Empty;
    
    public string Value
    {
        get => _value;
        set
        {
            if (value.Length > MaxLength)
            {
                throw new ArgumentException($"Maximum length of ECV key is {MaxLength}");
            }
            
            _value = value;
        }
    }

    public int GetBytesSize()
    {
        // kazdy znak (1bajt) + dlzka stringu (v int)
        return MaxLength + sizeof(byte);
    }

    public byte[] GetByteArray()
    {
        var stringBytes = Encoding.ASCII.GetBytes(_value);
        var byteArray = new byte[GetBytesSize()];
        
        // Ulozenie poctu zadanych znakov
        BitConverter.GetBytes(stringBytes.Length).CopyTo(byteArray, 0);
        
        // Ulozenie stringu
        stringBytes.CopyTo(byteArray, sizeof(byte));
        
        return byteArray;
    }
    
    public void FromByteArray(byte[] byteArray)
    {
        var usedBytes = byteArray[0];
        _value = Encoding.UTF8.GetString(byteArray, sizeof(byte), usedBytes);
    }

    public bool Equals(IEhfRecord? other)
    {
        return other is EcvKey fixedString &&
               _value == fixedString._value;
    }

    public BitArray GetHash()
    {
        var fullValue = _value.PadRight(MaxLength);
        var bytes = Encoding.ASCII.GetBytes(fullValue);
        var bitArray = new BitArray(bytes);
        var reversed = new BitArray(bitArray.Length);
        
        for (var i = 0; i < bitArray.Length; i++)
        {
            reversed[i] = bitArray[bitArray.Length - i - 1];
        }

        return reversed;
    }
}
