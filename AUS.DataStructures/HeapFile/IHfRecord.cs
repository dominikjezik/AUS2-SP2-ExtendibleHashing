using AUS.DataStructures.Shared;

namespace AUS.DataStructures.HeapFile;

public interface IHfRecord : ISerializable
{
    bool Equals(IHfRecord? other);
    
    void Update(IHfRecord record) { }
}
