using AUS.DataStructures.CarService;
using AUS.DataStructures.ExtendibleHashFile;

const string path = @"C:\Users\dominik\Desktop\TEST.dat";
//const string path = "/Users/dominik/Desktop/TEST.dat";

Console.WriteLine("Hello, World!");


File.Delete(path);
// File.Delete(@"/Users/dominik/Desktop/TEST.dir.dat");
File.Delete(@"C:\Users\dominik\Desktop\TEST.dir.dat");

List<int> ids = [
    0b00010,
    0b00110,
    0b01110,
    0b00011,
    0b00111,
    0b01111,
    0b11011,
];

// 28000
var ehf = new ExtendibleHashFile<Person>(path, 14000);

for (var i = 0; i < ids.Count; i++)
{
    var person = new Person
    {
        Id = ids[i],
        Ecv = $"ecv{i}",
        FirstName = $"John{i}",
        LastName = $"Doe{i}",
        ServiceVisits = []
    };

    ehf.Insert(person);
}

for (var i = 0; i < ids.Count; i++)
{
    var getPerson = new Person
    {
        Id = ids[i]
    };
    
    var person = ehf.Get(getPerson);
    
    Console.WriteLine($"{person?.Id} {person?.FirstName} {person?.LastName}");
}

//ehf.Delete(new Person { Id = 3 });

ehf.Close();
