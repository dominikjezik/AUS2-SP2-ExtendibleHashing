using System.Text;

namespace AUS.DataStructures.Shared;

public class FixedString : ISerializable
{
    private readonly int _maxLength;
    
    private string _value = string.Empty;
    
    public string Value
    {
        get => _value;
        set
        {
            // Ziskanie realnej dlzky (poctu znkaov) stringu UTF
            // Precital som si o tom tu: https://schneids.net/emojis-and-string-length
            var stringInfo = new System.Globalization.StringInfo(value);
            
            if (stringInfo.LengthInTextElements > _maxLength)
            {
                throw new ArgumentException($"Maximum length of fixed string is {_maxLength}");
            }
            
            _value = value;
        }
    }
    
    public FixedString(int maxLength)
    {
        _maxLength = maxLength;
    }

    public int GetBytesSize()
    {
        // kazdy znak * 4 bajty + dlzka stringu (v int)
        return _maxLength * 4 + sizeof(int);
    }

    public byte[] GetByteArray()
    {
        var stringBytes = Encoding.UTF8.GetBytes(_value);
        var byteArray = new byte[GetBytesSize()];
        
        // Ulozenie poctu zabranych bajtov stringu
        BitConverter.GetBytes(stringBytes.Length).CopyTo(byteArray, 0);
        
        // Ulozenie stringu
        stringBytes.CopyTo(byteArray, sizeof(int));
        
        return byteArray;
    }
    
    public void FromByteArray(byte[] byteArray)
    {
        var usedBytes = BitConverter.ToInt32(byteArray, 0);
        _value = Encoding.UTF8.GetString(byteArray, sizeof(int), usedBytes);
    }
}
