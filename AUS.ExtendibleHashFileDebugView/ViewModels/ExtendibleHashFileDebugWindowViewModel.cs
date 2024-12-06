using System.Collections.Generic;
using AUS.DataStructures.CarService;
using AUS.DataStructures.ExtendibleHashFile;

namespace AUS.ExtendibleHashFileDebugView.ViewModels;

public class ExtendibleHashFileDebugWindowViewModel : ViewModelBase
{
    private EhfDebug<Person> _ehfDebug;
    private bool _extendedView = false;

    public EhfDebug<Person> EhfDebug
    {
        get => _ehfDebug;
        set
        {
            _ehfDebug = value;
            DirectoryItems = new List<EhfDirectoryItem>(_ehfDebug.Directory);
            for (int i = 0; i < DirectoryItems.Count; i++)
            {
                DirectoryItems[i].DebugIndex = i;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(DirectoryItems));
        }
    }
    
    public List<EhfDirectoryItem> DirectoryItems { get; set; } = new();

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