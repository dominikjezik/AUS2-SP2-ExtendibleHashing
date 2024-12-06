using System.Collections.Generic;
using AUS.DataStructures.CarService;
using AUS.DataStructures.ExtendibleHashFile;

namespace AUS.GUI.ViewModels;

public class ExtendibleHashFileIdIndexDebugWindowViewModel : ExtendibleHashFileDebugView.ViewModels.ViewModelBase
{
    private EhfDebug<KeyToBlockAddress<PersonIdKey>> _ehfDebug = new();

    public EhfDebug<KeyToBlockAddress<PersonIdKey>> EhfDebug
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
}
