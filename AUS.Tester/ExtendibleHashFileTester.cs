using AUS.DataStructures.CarService;
using AUS.DataStructures.ExtendibleHashFile;

namespace AUS.Tester;

public class ExtendibleHashFileTester
{
    private readonly Random _random = new();
    private ExtendibleHashFile<Person> _ehf;
    private readonly List< Person> _helperList = [];
    private int _idCounter = 1;
    
    private readonly string _path;
    private readonly int _blockSize;
    private readonly bool _cleanAfterTest;

    public ExtendibleHashFileTester(string path, int blockSize, bool cleanAfterTest, int? seed = null)
    {
        _path = path;
        _blockSize = blockSize;
        _cleanAfterTest = cleanAfterTest;
        _ehf = new ExtendibleHashFile<Person>(path, blockSize);
        
        if (seed != null)
        {
            _random = new Random(seed.Value);
        }
    }

    public void Clean()
    {
        File.Delete(_path);
        
        Console.WriteLine("Testovaci subor bol vymazany");
    }
    
    public void CleanAndResetBeforeTest()
    {
        var directory = _ehf.GetDirectoryFileName();
        
        _ehf.Close();
        File.Delete(_path);
        
        File.Delete(directory);
        
        Console.WriteLine("Testovaci subor bol vymazany");
        
        _idCounter = 1;
        _helperList.Clear();
        
        _ehf = new ExtendibleHashFile<Person>(_path, _blockSize);
    }
    
    public void Close()
    {
        _ehf.Close();
    }
    
    public void TestRandomDataSet(int numberOfOperations, double probInsert)
    {
        for (int i = 0; i < numberOfOperations; i++)
        {
            var prob = _random.NextDouble();

            if (prob <= probInsert)
            {
                var id = _random.Next(1, int.MaxValue);
                
                while (_helperList.Any(x => x.Id == id))
                {
                    id = _random.Next(1, int.MaxValue);
                }
                
                TestInsert(id);
            }
            else
            {
                TestDelete();
            }

            TestFindEveryItem();
            TestEveryEmptyBlockIsInFreeBlockChainAndTestLastBlock();
        }

        _ehf.Close();

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
            var id = _random.Next(1, int.MaxValue);
                
            while (_helperList.Any(x => x.Id == id))
            {
                id = _random.Next(1, int.MaxValue);
            }
                
            TestInsert(id);
            TestFindEveryItem();
        }

        for (int i = 0; i < 99; i++)
        {
            TestDelete();
            TestFindEveryItem();
            TestEveryEmptyBlockIsInFreeBlockChainAndTestLastBlock();
        }
        
        _ehf.Close();
        
        Console.WriteLine("Test uspesne ukonceny");
        
        if (_cleanAfterTest)
        {
            Clean();
        }
    }
    
    public void TestIncreasingKeyAndRandomDataSet(int numberOfOperations, double probInsert)
    {
        for (int i = 0; i < numberOfOperations; i++)
        {
            var prob = _random.NextDouble();

            if (prob <= probInsert)
            {
                TestInsert(_idCounter++);
            }
            else
            {
                TestDelete();
            }

            TestFindEveryItem();
            TestEveryEmptyBlockIsInFreeBlockChainAndTestLastBlock();
        }
        
        _ehf.Close();
        
        Console.WriteLine("Test uspesne ukonceny");
        
        if (_cleanAfterTest)
        {
            Clean();
        }
    }

    public void TestInsert(int id)
    {
        var numberOfServiceVisits = _random.Next(0, 5);
        
        var person = new Person
        {
            Id = id,
            Ecv = $"ECV{id}",
            FirstName = $"John{_random.Next(0, 1000)}",
            LastName = $"Doe{_random.Next(0, 1000)}"
        };
        
        Console.WriteLine($"[INSERT] Vygenerovany prvok pre vlozenie {person}");
        
        _ehf.Insert(person);
        _helperList.Add(person);
        
        Console.WriteLine($"Prvok {person} vlozeny pod klucom {person.Id}");
        
        // Kontrola ci tam naozaj je vlozeny a ci ho dokazem vybrat
        var insertedData = _ehf.Get(new Person { Id = person.Id });
        
        if (insertedData == null || insertedData.Id != person.Id || insertedData.FirstName != person.FirstName || insertedData.LastName != person.LastName)
        {
            throw new Exception("Bola volana operacia insert ale find nenasiel polozku");
        }
    }

    public void TestDelete()
    {
        if (_helperList.Count > 0)
        {
                    
            var index = _random.Next(0, _helperList.Count);
            var person = _helperList[index];

            _ehf.Delete(person);
            _helperList.RemoveAt(index);

            Console.WriteLine($"[DELETE] Vygenerovany prvok pre vymazanie {person}");
        }
        else
        {
            Console.WriteLine("Bola vygenerovana operacia delete ale ehf je momentalne prazdny");
        }
    }
    
    public void TestFindEveryItem()
    {
        foreach (var data in _helperList)
        {
            var insertedData = _ehf.Get(new Person { Id = data.Id });
        
            if (insertedData == null || insertedData.Id != data.Id || insertedData.FirstName != data.FirstName || insertedData.LastName != data.LastName)
            {
                _ehf.Close();
                throw new Exception($"Bola volana operacia insert nad klucom {data.Id} ale find nenasiel polozku");
            }
        }
    }

    public void TestEveryEmptyBlockIsInFreeBlockChainAndTestLastBlock()
    {
        var debugObject = _ehf.GetDebugObject();
        for (var i = 0; i < debugObject.Blocks.Count; i++)
        {
            var block = debugObject.Blocks[i];
            var used = block.StoredRecords.Count(x => x.Id != 0);

            if (used == 0 && block.NextFreeBlockAddress == -1 && block.PreviousFreeBlockAddress == -1 && block.BlockAddress != debugObject.FirstFreeBlockAddress)
            {
                _ehf.Close();
                throw new Exception("Bol najdeny prazdny blok, ktory nie je zaradeny v zretazeni volnych blokov!");
            }

            // Kontrola posledneho bloku => nemal by byt prazdny
            if (i == debugObject.Blocks.Count - 1 && block.StoredRecords.All(x => x.Id == 0))
            {
                _ehf.Close();
                throw new Exception("Posledny blok v subore je prazdny");
            }
        }
    }
}
