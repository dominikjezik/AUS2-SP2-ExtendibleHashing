namespace AUS.DataStructures.Shared;

public class SerializableDate : ISerializable
{
    public DateTime Value { get; set; }

    public int GetBytesSize()
    {
        return sizeof(int);
    }

    public byte[] GetByteArray()
    {
        var bytes = new byte[GetBytesSize()];
        var start = new DateTime(1970, 1, 1);
        
        var timeBetweenStartAndValue = Value.Date - start;
        var seconds = (int)timeBetweenStartAndValue.TotalDays;
        
        BitConverter.GetBytes(seconds).CopyTo(bytes, 0);
        
        return bytes;
    }

    public void FromByteArray(byte[] byteArray)
    {
        var days = BitConverter.ToInt32(byteArray, 0);
        Value = new DateTime(1970, 1, 1).AddDays(days);
    }
}
