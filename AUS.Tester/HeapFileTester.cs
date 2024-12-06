using AUS.DataStructures.CarService;
using AUS.DataStructures.HeapFile;

namespace AUS.Tester;

public class HeapFileTester
{
    private readonly Random _random = new();
    private HeapFile<Person> _heapFile;
    private readonly List<(long, Person)> _helperList = [];
    private int _idCounter = 1;
    
    private readonly string _path;
    private readonly int _blockSize;
    private readonly bool _cleanAfterTest;

    public HeapFileTester(string path, int blockSize, bool cleanAfterTest)
    {
        _path = path;
        _blockSize = blockSize;
        _cleanAfterTest = cleanAfterTest;
        _heapFile = new HeapFile<Person>(path, blockSize);
    }

    public void Clean()
    {
        File.Delete(_path);
        
        Console.WriteLine("Testovaci subor bol vymazany");
    }
    
    public void CleanAndResetBeforeTest()
    {
        _heapFile.Close();
        File.Delete(_path);
        
        Console.WriteLine("Testovaci subor bol vymazany");
        
        _idCounter = 1;
        _helperList.Clear();
        
        _heapFile = new HeapFile<Person>(_path, _blockSize);
    }
    
    public void Close()
    {
        _heapFile.Close();
    }
    
    public void TestRandomDataSet(int numberOfOperations, double probInsert)
    {
        for (int i = 0; i < numberOfOperations; i++)
        {
            var prob = _random.NextDouble();

            if (prob <= probInsert)
            {
                TestInsert();
            }
            else
            {
                TestDelete();
            }

            TestFindEveryItem();
        }
        
        _heapFile.Close();
        
        Console.WriteLine("Test uspesne ukonceny");
        
        if (_cleanAfterTest)
        {
            Clean();
        }
    }
    
    public void TestFullInsertThenFullDelete()
    {
        for (int i = 0; i < 100; i++)
        {
            Console.WriteLine($"[I] {i}");
            TestInsert();
            TestFindEveryItem();
            TestLastBlockIsNotEmpty();
        }

        for (int i = 0; i < 99; i++)
        {
            Console.WriteLine($"[D] {i}");
            TestDelete();
            TestFindEveryItem();
            
            if (i != 98)
            {
                TestLastBlockIsNotEmpty();
            }
        }
        
        _heapFile.Close();
        
        Console.WriteLine("Test uspesne ukonceny");
        
        if (_cleanAfterTest)
        {
            Clean();
        }
    }
    
    public void TestInsert()
    {
        var serviceVisits = new List<ServiceVisit>();

        var id = _idCounter++;
        
        var person = new Person
        {
            Id = id,
            Ecv = $"ECV{id}",
            FirstName = $"John{_random.Next(0, 1000)}",
            LastName = $"Doe{_random.Next(0, 1000)}",
            ServiceVisits = serviceVisits.ToArray()
        };
        
        Console.WriteLine($"[INSERT] Vygenerovany prvok pre vlozenie {person}");
        
        var key = _heapFile.Insert(person);
        _helperList.Add((key, person));
        
        Console.WriteLine($"Prvok {person} vlozeny pod klucom {key}");
        
        // Kontrola ci tam naozaj je vlozeny a ci ho dokazem vybrat
        var insertedData = _heapFile.Get(key, new Person { Id = person.Id });
        
        if (insertedData == null || insertedData.Id != person.Id || insertedData.FirstName != person.FirstName || insertedData.LastName != person.LastName)
        {
            throw new Exception("Bola volana operacia insert ale find nenasiel polozku");
        }
    }

    public void TestDelete()
    {
        if (_helperList.Count == 0)
        {
            Console.WriteLine("Pokus o vymazanie ale strom je momentalne prazdny");
            return;
        }
        
        var index = _random.Next(_helperList.Count);
        var (key, data) = _helperList[index];
        Console.WriteLine($"[DELETE] Vybrany prvok pre vymazanie {data} s klucom {key}");

        _heapFile.Delete(key, new Person { Id = data.Id });
        
        Console.WriteLine($"Prvok {data} vymazany s klucom {key}");
        
        _helperList.RemoveAt(index);
        
        // Kontrola ci tam naozaj nie je vlozeny ked som ho vymazal
        var insertedData = _heapFile.Get(key, new Person { Id = data.Id });
        
        if (insertedData != null)
        {
            throw new Exception("Bola volana operacia delete ale find nasiel polozku");
        }
    }
    
    public void TestFindEveryItem()
    {
        foreach (var (key, data) in _helperList)
        {
            var insertedData = _heapFile.Get(key, new Person { Id = data.Id });
        
            if (insertedData == null || insertedData.Id != data.Id || insertedData.FirstName != data.FirstName || insertedData.LastName != data.LastName)
            {
                throw new Exception($"Bola volana operacia insert nad klucom {key} ale find nenasiel polozku / nenasiel spravnu polozku");
            }
        }
    }
    
    public void TestLastBlockIsNotEmpty()
    {
        var lastBlock = _heapFile.GetLastBlockForDebug();
        
        if (lastBlock.StoredRecords.All(x => x.Id == 0))
        {
            throw new Exception("Posledny blok je prazdny");
        }
    }
}
