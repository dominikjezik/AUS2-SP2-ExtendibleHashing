using AUS.DataStructures.CarService;
using AUS.DataStructures.HeapFile;

namespace AUS.HeapFileDebugView.ViewModels;

public class HeapFileDebugWindowViewModel : ViewModelBase
{
    private bool _extendedView = false;
    private HfDebug<Person> _hfDebug;
    
    public HfDebug<Person> HfDebug
    {
        get => _hfDebug;
        set
        {
            _hfDebug = value;
            OnPropertyChanged();
        }
    }

    public bool CompactView => !ExtendedView;
    
    public bool ExtendedView
    {
        get => _extendedView;
        set
        {
            _extendedView = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CompactView));
        }
    }
}
