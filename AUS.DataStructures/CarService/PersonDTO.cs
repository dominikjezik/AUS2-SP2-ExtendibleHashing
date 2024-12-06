using System.Collections.ObjectModel;

namespace AUS.DataStructures.CarService;

public class PersonDTO
{
    public int Id { get; set; } = 0;
    
    public string ECV { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public ObservableCollection<ServiceVisitDTO> ServiceVisits { get; set; } = new();
}

public static class PersonDTOExtensions
{
    public static PersonDTO ToDTO(this Person person)
    {
        var personDTO =  new PersonDTO
        {
            Id = person.Id.Value,
            ECV = person.Ecv,
            FirstName = person.FirstName,
            LastName = person.LastName
        };
        
        foreach (var serviceVisit in person.ServiceVisits)
        {
            if (serviceVisit != null)
            {
                personDTO.ServiceVisits.Add(serviceVisit.ToDTO());
            }
        }
        
        return personDTO;
    }
    
    public static Person ToPerson(this PersonDTO personDTO)
    {
        var firstNameInfo = new System.Globalization.StringInfo(personDTO.FirstName);
        
        if (firstNameInfo.LengthInTextElements > 15)
        {
            personDTO.FirstName = firstNameInfo.SubstringByTextElements(0, 15);
        }
        
        var lastNameInfo = new System.Globalization.StringInfo(personDTO.LastName);
        
        if (lastNameInfo.LengthInTextElements > 20)
        {
            personDTO.LastName = lastNameInfo.SubstringByTextElements(0, 20);
        }
        
        var ecvInfo = new System.Globalization.StringInfo(personDTO.ECV);
        
        if (ecvInfo.LengthInTextElements > 20)
        {
            personDTO.ECV = ecvInfo.SubstringByTextElements(0, 20);
        }
        
        return new Person
        {
            Id = personDTO.Id,
            Ecv = personDTO.ECV,
            FirstName = personDTO.FirstName,
            LastName = personDTO.LastName,
            ServiceVisits = personDTO.ServiceVisits.Select(s => s.ToServiceVisit()).ToArray()
        };
    }
}
