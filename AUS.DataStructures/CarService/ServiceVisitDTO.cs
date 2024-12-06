namespace AUS.DataStructures.CarService;

public class ServiceVisitDTO
{
    public DateTime Date { get; set; }
    
    public double Price { get; set; } = 0;
    
    public string FullDescription { get; set; } = string.Empty;
}

public static class ServiceVisitDTOExtensions
{
    public static ServiceVisitDTO ToDTO(this ServiceVisit serviceVisit)
    {
        var fullDescription = string.Join("\n", serviceVisit.Description).TrimEnd();
        
        return new ServiceVisitDTO
        {
            Date = serviceVisit.Date,
            Price = serviceVisit.Price,
            FullDescription = fullDescription
        };
    }
    
    public static ServiceVisit ToServiceVisit(this ServiceVisitDTO personDTO)
    {
        var descriptionArray = personDTO.FullDescription
            .Split("\n")
            .Take(10)
            .Select(x => x.Length > 20 ? x.Substring(0, 20) : x)
            .ToArray();
        
        return new ServiceVisit
        {
            Date = personDTO.Date,
            Price = personDTO.Price,
            Description = descriptionArray
        };
    }
}
