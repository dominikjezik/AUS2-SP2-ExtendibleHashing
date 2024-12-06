using System.Text;
using AUS.DataStructures.ExtendibleHashFile;
using AUS.DataStructures.HeapFile;

namespace AUS.DataStructures.CarService;

public class ApplicationService
{
    private readonly HeapFile<Person> _dataHeapFile;
    
    private readonly ExtendibleHashFile<KeyToBlockAddress<EcvKey>> _indexByEcvEhf;
    
    private readonly ExtendibleHashFile<KeyToBlockAddress<PersonIdKey>> _indexByPersonIdEhf;
    
    private readonly Random _random = new();
    
    public ApplicationService(string dbBaseFile, int dataBlockSize, int indexBlockSize)
    {
        var dataFilePath = $"{dbBaseFile}/data.hf.dat";
        _dataHeapFile = new(dataFilePath, dataBlockSize);
        
        var indexByEcvFilePath = $"{dbBaseFile}/indexByEcv.ehf.dat";
        _indexByEcvEhf = new(indexByEcvFilePath, indexBlockSize);
        
        var indexByPersonIdFilePath = $"{dbBaseFile}/indexByPersonId.ehf.dat";
        _indexByPersonIdEhf = new(indexByPersonIdFilePath, indexBlockSize);
    }
    
    public void Close()
    {
        _dataHeapFile.Close();
        _indexByEcvEhf.Close();
        _indexByPersonIdEhf.Close();
    }

    public PersonDTO? Get(PersonQuery query)
    {
        if (query.SearchBy == "ID")
        {
            int.TryParse(query.SearchValue, out var id);
            
            var keyToBlockAddress = new KeyToBlockAddress<PersonIdKey>
            {
                Key = new PersonIdKey { Value = id }
            };
            
            var foundKeyToBlockAddress = _indexByPersonIdEhf.Get(keyToBlockAddress);
            
            if (foundKeyToBlockAddress == null)
            {
                return null;
            }
            
            var blockAddress = foundKeyToBlockAddress.BlockAddress;
            
            var person = _dataHeapFile.Get(blockAddress, new Person { Id = id });
            
            return person?.ToDTO();
        }
        else
        {
            var keyToBlockAddress = new KeyToBlockAddress<EcvKey>
            {
                Key = new EcvKey { Value = query.SearchValue } 
            };
            
            var foundKeyToBlockAddress = _indexByEcvEhf.Get(keyToBlockAddress);
            
            if (foundKeyToBlockAddress == null)
            {
                return null;
            }
            
            var blockAddress = foundKeyToBlockAddress.BlockAddress;
            
            var person = _dataHeapFile.Get(blockAddress, new Person { Ecv = query.SearchValue, Id = null });
            
            return person?.ToDTO();
        }
    }
    
    public void Insert(PersonDTO personDTO)
    {
        // Kontrola ci uz neexistuje zaznam s rovnakym ID alebo ECV
        var keyToBlockAddressByPersonIdCheck = new KeyToBlockAddress<PersonIdKey>
        {
            Key = new PersonIdKey { Value = personDTO.Id }
        };
        
        if (_indexByPersonIdEhf.Get(keyToBlockAddressByPersonIdCheck) != null)
        {
            throw new Exception($"Osoba s ID {personDTO.Id} uz existuje");
        }
        
        var keyToBlockAddressByEcvCheck = new KeyToBlockAddress<EcvKey>
        {
            Key = new EcvKey { Value = personDTO.ECV }
        };
        
        if (_indexByEcvEhf.Get(keyToBlockAddressByEcvCheck) != null)
        {
            throw new Exception($"Osoba s ECV {personDTO.ECV} uz existuje");
        }
        
        var personToInsert = personDTO.ToPerson();
        var blockAddress = _dataHeapFile.Insert(personToInsert);
        
        var keyToBlockAddressByPersonId = new KeyToBlockAddress<PersonIdKey>
        {
            Key = new PersonIdKey { Value = personToInsert.Id.Value },
            BlockAddress = blockAddress
        };
        
        _indexByPersonIdEhf.Insert(keyToBlockAddressByPersonId);
        
        var keyToBlockAddressByEcv = new KeyToBlockAddress<EcvKey>
        {
            Key = new EcvKey { Value = personToInsert.Ecv },
            BlockAddress = blockAddress
        };
        
        _indexByEcvEhf.Insert(keyToBlockAddressByEcv);
    }

    public void Delete(int selectedPersonOriginalId, string selectedPersonOriginalEcv)
    {
        var keyToBlockAddress = new KeyToBlockAddress<PersonIdKey>
        {
            Key = new PersonIdKey { Value = selectedPersonOriginalId }
        };
        
        var foundKeyToBlockAddress = _indexByPersonIdEhf.Get(keyToBlockAddress);
        
        if (foundKeyToBlockAddress == null)
        {
            throw new Exception("Person not found");
        }
        
        var blockAddress = foundKeyToBlockAddress.BlockAddress;
        
        _dataHeapFile.Delete(blockAddress, new Person { Id = selectedPersonOriginalId });
        
        _indexByPersonIdEhf.Delete(keyToBlockAddress);
        
        var keyToBlockAddressByEcv = new KeyToBlockAddress<EcvKey>
        {
            Key = new EcvKey { Value = selectedPersonOriginalEcv }
        };
        
        _indexByEcvEhf.Delete(keyToBlockAddressByEcv);
    }
    
    public void Update(int selectedPersonOriginalId, string selectedPersonOriginalEcv, PersonDTO selectedPerson)
    {
        // Kontrola v pripade ze sa zmenili klucove atributy, ci uz neexistuje zaznam s rovnakym ID alebo ECV
        if (selectedPersonOriginalId != selectedPerson.Id)
        {
            var keyToBlockAddressByPersonIdCheck = new KeyToBlockAddress<PersonIdKey>
            {
                Key = new PersonIdKey { Value = selectedPerson.Id }
            };
            
            if (_indexByPersonIdEhf.Get(keyToBlockAddressByPersonIdCheck) != null)
            {
                throw new Exception($"Osoba s ID {selectedPerson.Id} uz existuje");
            }
        }
        
        if (selectedPersonOriginalEcv != selectedPerson.ECV)
        {
            var keyToBlockAddressByEcvCheck = new KeyToBlockAddress<EcvKey>
            {
                Key = new EcvKey { Value = selectedPerson.ECV }
            };
            
            if (_indexByEcvEhf.Get(keyToBlockAddressByEcvCheck) != null)
            {
                throw new Exception($"Osoba s ECV {selectedPerson.ECV} uz existuje");
            }
        }
        
        var updatedPerson = selectedPerson.ToPerson();
        
        var keyToBlockAddress = new KeyToBlockAddress<PersonIdKey>
        {
            Key = new PersonIdKey { Value = selectedPersonOriginalId }
        };
        
        var foundKeyToBlockAddress = _indexByPersonIdEhf.Get(keyToBlockAddress);
        
        if (foundKeyToBlockAddress == null)
        {
            throw new Exception("Person not found");
        }
        
        var blockAddress = foundKeyToBlockAddress.BlockAddress;
        
        _dataHeapFile.Update(blockAddress, new Person { Id = selectedPersonOriginalId }, updatedPerson);
        
        // Kontrola ci sa nezmenili klucove atributy
        if (selectedPersonOriginalId != updatedPerson.Id)
        {
            _indexByPersonIdEhf.Delete(keyToBlockAddress);
            _indexByPersonIdEhf.Insert(new KeyToBlockAddress<PersonIdKey>
            {
                Key = new PersonIdKey { Value = updatedPerson.Id.Value },
                BlockAddress = blockAddress
            });
        }
        
        if (selectedPersonOriginalEcv != updatedPerson.Ecv)
        {
            _indexByEcvEhf.Delete(new KeyToBlockAddress<EcvKey>
            {
                Key = new EcvKey { Value = selectedPersonOriginalEcv }
            });
            
            _indexByEcvEhf.Insert(new KeyToBlockAddress<EcvKey>
            {
                Key = new EcvKey { Value = updatedPerson.Ecv },
                BlockAddress = blockAddress
            });
        }
    }
    
    #region GeneratePersons
    
    public void GeneratePersons(GenerateOptions options)
    {
        for (var i = 0; i < options.CountOfPersons; i++)
        {
            if (options.MinCountOfVisits > 5)
            {
                options.MinCountOfVisits = 5;
            }
            
            if (options.MaxCountOfVisits > 5)
            {
                options.MaxCountOfVisits = 5;
            }
            
            if (options.MinCountOfVisits < 0)
            {
                options.MinCountOfVisits = 0;
            }
            
            if (options.MaxCountOfVisits < 0)
            {
                options.MaxCountOfVisits = 0;
            }
            
            if (options.MinCountOfVisits > options.MaxCountOfVisits)
            {
                options.MinCountOfVisits = 0;
                options.MaxCountOfVisits = 0;
            }
            
            var countOfServiceVisits = _random.Next(options.MinCountOfVisits, options.MaxCountOfVisits + 1);
            var serviceVisits = new ServiceVisit[5];
    
            for (var j = 0; j < countOfServiceVisits; j++)
            {
                serviceVisits[j] = new ServiceVisit
                {
                    Date = DateTime.Now,
                    Description = [ "Popis návštevy" ]
                };
            }
            
            var person = new Person
            {
                Id = _random.Next(1, int.MaxValue),
                Ecv = GenerateEcv(),
                FirstName = "Meno",
                LastName = "Priezvisko",
                ServiceVisits = serviceVisits
            };
            
            Insert(person.ToDTO());
        }
    }

    private string GenerateEcv()
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var lettersAndNumber = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        var ecv = new StringBuilder();
        
        ecv.Append(letters[_random.Next(0, letters.Length)]);
        ecv.Append(letters[_random.Next(0, letters.Length)]);
        
        for (var i = 0; i < 5; i++)
        {
            ecv.Append(lettersAndNumber[_random.Next(0, lettersAndNumber.Length)]);
        }
        
        return ecv.ToString();
    }
    
    #endregion

    #region Debug

    public HfDebug<Person> GetDebugDataHeapFile() => _dataHeapFile.GetDebugObject();

    public EhfDebug<KeyToBlockAddress<PersonIdKey>> GetDebugIndexByPersonIdEhf() => _indexByPersonIdEhf.GetDebugObject();
    
    public EhfDebug<KeyToBlockAddress<EcvKey>> GetDebugIndexByEcvEhf() => _indexByEcvEhf.GetDebugObject();

    #endregion
}
