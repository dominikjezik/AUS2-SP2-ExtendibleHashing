namespace AUS.DataStructures.Shared;

public interface ISerializable
{
    int GetBytesSize();
    byte[] GetByteArray();
    void FromByteArray(byte[] byteArray);
}
