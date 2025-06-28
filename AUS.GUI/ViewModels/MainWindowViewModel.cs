using System.Collections.Generic;
using AUS.DataStructures.CarService;

namespace AUS.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    /*
    private const int DataBlockSize = 28000;
    private const int IndexBlockSize = 500;
    */
    
    //private const int DataBlockSize = 10000;
    //private const int IndexBlockSize = 550;
    private const int DataBlockSize = 24000;
    private const int IndexBlockSize = 220;
    
    //private const string DBBasePath = "/Users/dominik/Desktop/data";
    private const string DBBasePath = @"C:\Users\dominik\Desktop\data";
    
    private ApplicationService _service;
    
    private PersonDTO? _selectedPerson;
    
    private int _selectedPersonOriginalId;
    
    private string _selectedPersonOriginalEcv;
    
    private ServiceVisitDTO? _selectedServiceVisit;

    public ApplicationService Service => _service;
    
    public List<string> SearchByOptions { get; set; } = ["ID", "License Plate"];
    
    public PersonQuery PersonQuery { get; set; } = new() { SearchBy = "ID" };

    public PersonDTO? SelectedPerson
    {
        get => _selectedPerson;
        set
        {
            _selectedPerson = value;
            _selectedPersonOriginalId = value?.Id ?? -1;
            _selectedPersonOriginalEcv = value?.ECV ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSelectedPerson));
            OnPropertyChanged(nameof(IsEnabledAddServiceVisit));
        }
    }
    
    public bool IsSelectedPerson => SelectedPerson != null;

    public ServiceVisitDTO? SelectedServiceVisit
    {
        get => _selectedServiceVisit;
        set
        {
            _selectedServiceVisit = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSelectedServiceVisit));
        }
    }
    
    public bool IsSelectedServiceVisit => SelectedServiceVisit != null;
    
    public bool IsEnabledAddServiceVisit => SelectedPerson != null && SelectedPerson.ServiceVisits.Count < 5;
    
    public MainWindowViewModel()
    {
        _service = new ApplicationService(DBBasePath, DataBlockSize, IndexBlockSize);
    }
    
    public void GeneratePersons(GenerateOptions options)
    {
        _service.GeneratePersons(options);
    }

    public void DeleteSelectedPerson()
    {
        _service.Delete(_selectedPersonOriginalId, _selectedPersonOriginalEcv);
        SelectedServiceVisit = null;
        SelectedPerson = null;
    }

    public void UpdateSelectedPerson()
    {
        _service.Update(_selectedPersonOriginalId, _selectedPersonOriginalEcv, SelectedPerson);
        SelectedServiceVisit = null;
        SelectedPerson = null;
    }
    
    public ServiceVisitDTO InsertNewServiceVisit()
    {
        var newServiceVisit = new ServiceVisitDTO();
        
        SelectedPerson.ServiceVisits.Add(newServiceVisit);
        SelectedServiceVisit = newServiceVisit;
        
        OnPropertyChanged(nameof(IsEnabledAddServiceVisit));
        
        return newServiceVisit;
    }

    public void DeleteSelectedServiceVisit()
    {
        SelectedPerson.ServiceVisits.Remove(SelectedServiceVisit);
        SelectedServiceVisit = null;
        
        OnPropertyChanged(nameof(SelectedPerson));
        OnPropertyChanged(nameof(IsEnabledAddServiceVisit));
    }
}
