using System.Collections;
using AUS.DataStructures.Shared;

namespace AUS.DataStructures.ExtendibleHashFile;

public interface IEhfRecord : ISerializable
{
    bool Equals(IEhfRecord? other);
    
    public BitArray GetHash();
}
