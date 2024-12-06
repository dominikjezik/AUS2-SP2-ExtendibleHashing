using AUS.DataStructures.Shared;

namespace AUS.DataStructures.CarService;

public class ServiceVisit : ISerializable
{
    private SerializableDate _date = new();
    
    private FixedString[] _description = new FixedString[10];
    
    public DateTime Date
    {
        get => _date.Value;
        set => _date.Value = value;
    }
    
    public double Price { get; set; }

    public string[] Description
    {
        get
        {
            return _description.Select(x => x.Value).ToArray();
        }
        set
        {
            _description = new FixedString[_description.Length];
            for (var i = 0; i < _description.Length; i++)
            {
                _description[i] = new FixedString(20);
            }
            
            for (var i = 0; i < value.Length; i++)
            {
                _description[i].Value = value[i];
            }
        }
    }
    
    public string DescriptionFirstLineView => _description.FirstOrDefault()?.Value ?? string.Empty;

    public ServiceVisit()
    {
        for (var i = 0; i < _description.Length; i++)
        {
            _description[i] = new FixedString(20);
        }
    }
    
    public int GetBytesSize()
    {
        return _date.GetBytesSize() + sizeof(double) + _description.Sum(x => x.GetBytesSize());
    }

    public byte[] GetByteArray()
    {
        var bytes = new byte[GetBytesSize()];
        
        _date.GetByteArray().CopyTo(bytes, 0);
        
        BitConverter.GetBytes(Price).CopyTo(bytes, _date.GetBytesSize());
        
        for (var i = 0; i < _description.Length; i++)
        {
            _description[i].GetByteArray().CopyTo(bytes, _date.GetBytesSize() + sizeof(double) + i * _description[i].GetBytesSize());
        }
        
        return bytes;
    }

    public void FromByteArray(byte[] byteArray)
    {
        _date.FromByteArray(byteArray[0.._date.GetBytesSize()]);
        
        Price = BitConverter.ToDouble(byteArray[_date.GetBytesSize()..(_date.GetBytesSize() + sizeof(double))]);
        
        var currentByte = _date.GetBytesSize() + sizeof(double);
        
        for (var i = 0; i < _description.Length; i++)
        {
            _description[i].FromByteArray(byteArray[currentByte..(currentByte + _description[i].GetBytesSize())]);
            currentByte += _description[i].GetBytesSize();
        }
    }
}
