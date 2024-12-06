using System.Collections;
using AUS.DataStructures.ExtendibleHashFile;
using AUS.DataStructures.HeapFile;
using AUS.DataStructures.Shared;

namespace AUS.DataStructures.CarService;

public class Person : IEhfRecord, IHfRecord
{
    private FixedString _firstName = new(15);
    
    private FixedString _lastName = new(20);
    
    private FixedString _ecv = new(20);

    public int? Id { get; set; } = 0;

    public string FirstName
    {
        get => _firstName.Value;
        set => _firstName.Value = value;
    }

    public string LastName
    {
        get => _lastName.Value;
        set => _lastName.Value = value;
    }
    
    public string Ecv
    {
        get => _ecv.Value;
        set => _ecv.Value = value;
    }
    
    public ServiceVisit?[] ServiceVisits { get; set; } = new ServiceVisit[5];
    
    public int ServiceVisitsCount => ServiceVisits.Count(x => x != null);
    
    private readonly int _serviceVisitItemBytesSize = new ServiceVisit().GetBytesSize();

    public int GetBytesSize()
    {
        // Id + FirstName + LastName + Ecv + ServiceVisitsCount + ServiceVisits
        return sizeof(int) + _firstName.GetBytesSize() + _lastName.GetBytesSize() + _ecv.GetBytesSize() + sizeof(byte) + 5 * _serviceVisitItemBytesSize;
    }
    
    public bool Equals(IEhfRecord? other) => EqualsTo(other);
    
    public bool Equals(IHfRecord? other) => EqualsTo(other);

    private bool EqualsTo(object? other)
    {
        return other is Person person &&
               (Id == person.Id || Ecv == person.Ecv);
    }

    public BitArray GetHash()
    {
        // Vratime bity v opacnom poradi => lepsie ak su cisla mensie (lebo nie su na zaciatku same nuly)
        var bitArray = new BitArray(BitConverter.GetBytes(Id!.Value));
        var reversed = new BitArray(bitArray.Length);
        
        for (var i = 0; i < bitArray.Length; i++)
        {
            reversed[i] = bitArray[bitArray.Length - i - 1];
        }
        
        return reversed;
    }
    
    public byte[] GetByteArray()
    {
        var bytes = new byte[GetBytesSize()];
        
        BitConverter.GetBytes(Id ?? 0).CopyTo(bytes, 0);
        
        _ecv.GetByteArray().CopyTo(bytes, sizeof(int));
        
        _firstName.GetByteArray().CopyTo(bytes, sizeof(int) + _ecv.GetBytesSize());
        _lastName.GetByteArray().CopyTo(bytes, sizeof(int) + _ecv.GetBytesSize() + _firstName.GetBytesSize());
        
        // Zaevidovanie poctu navstev
        bytes[sizeof(int) + _ecv.GetBytesSize() + _firstName.GetBytesSize() + _lastName.GetBytesSize()] = (byte)ServiceVisitsCount;
        
        // Zaevidovanie navstev
        for (var i = 0; i < ServiceVisits.Length; i++)
        {
            if (ServiceVisits[i] == null)
            {
                continue;
            }
            
            ServiceVisits[i].GetByteArray().CopyTo(bytes, sizeof(int) + _ecv.GetBytesSize() + _firstName.GetBytesSize() + _lastName.GetBytesSize() + sizeof(byte) + i * _serviceVisitItemBytesSize);
        }
        
        // Nerealizovane navstevy su vyplnene nulami
        for (var i = ServiceVisits.Length; i < 5; i++)
        {
            for (var j = 0; j < _serviceVisitItemBytesSize; j++)
            {
                bytes[sizeof(int) + _ecv.GetBytesSize() + _firstName.GetBytesSize() + _lastName.GetBytesSize() + sizeof(byte) + i * _serviceVisitItemBytesSize + j] = 0;
            }
        }
        
        return bytes;
    }

    public void FromByteArray(byte[] byteArray)
    {
        
        int currentOffset = 0;
        
        Id = BitConverter.ToInt32(byteArray, 0);
        
        currentOffset += sizeof(int);
        
        _ecv.FromByteArray(byteArray[currentOffset..(currentOffset + _ecv.GetBytesSize())]);
        
        currentOffset += _ecv.GetBytesSize();

        _firstName.FromByteArray(byteArray[currentOffset..(currentOffset + _firstName.GetBytesSize())]);

        currentOffset += _firstName.GetBytesSize();

        _lastName.FromByteArray(byteArray[currentOffset..(currentOffset + _lastName.GetBytesSize())]);

        currentOffset += _lastName.GetBytesSize();

        var serviceVisitsCount = byteArray[currentOffset];

        currentOffset += sizeof(byte);

        for (var i = 0; i < serviceVisitsCount; i++)
        {
            var serviceVisit = new ServiceVisit();

            serviceVisit.FromByteArray(byteArray[currentOffset..(currentOffset + _serviceVisitItemBytesSize)]);

            currentOffset += _serviceVisitItemBytesSize;

            ServiceVisits[i] = serviceVisit;
        }

        for (var i = serviceVisitsCount; i < ServiceVisits.Length; i++)
        {
            ServiceVisits[i] = null;
        }
    }

    public override string ToString()
    {
        return $"Id: {Id}, FirstName: {FirstName}, LastName: {LastName}, ServiceVisitsCount: {ServiceVisitsCount}";
    }

    public void Update(IHfRecord record)
    {
        if (record is Person person)
        {
            Id = person.Id;
            FirstName = person.FirstName;
            LastName = person.LastName;
            Ecv = person.Ecv;
            ServiceVisits = person.ServiceVisits;
        }
    }
}
