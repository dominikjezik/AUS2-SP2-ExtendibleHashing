using AUS.DataStructures.CarService;
using AUS.DataStructures.ExtendibleHashFile;
using AUS.ExtendibleHashFileDebugView.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AUS.ExtendibleHashFileDebugView.Views;

public partial class ExtendibleHashFileDebugWindow : Window
{
    // 28000, 14000
    private const int DefaultBlockSize = 28000;
    //private const string DefaultFileName = @"C:\Users\dominik\Desktop\extendibleHashFile.dat";
    private const string DefaultFileName = @"C:\Users\dominik\Desktop\TEST.dat";
    //private const string DefaultFileName = "/Users/dominik/Desktop/extendibleHashFile.dat";
    //private const string DefaultFileName = "/Users/dominik/Desktop/TEST.dat";
    
    private readonly string _fileName = DefaultFileName;
    private readonly int _blockSize = DefaultBlockSize;
    
    private bool _openedStandalone = true;
    private ExtendibleHashFile<Person> _ehf = null!;
    
    public ExtendibleHashFileDebugWindow()
    {
        InitializeComponent();
        DataContext = new ExtendibleHashFileDebugWindowViewModel();
    }
    
    public ExtendibleHashFileDebugWindow(int blockSize, string fileName)
    {
        InitializeComponent();
        DataContext = new ExtendibleHashFileDebugWindowViewModel();
        
        _blockSize = blockSize;
        _fileName = fileName;
    }
    
    public ExtendibleHashFileDebugWindow(EhfDebug<Person> ehfDebug)
    {
        InitializeComponent();
        DataContext = new ExtendibleHashFileDebugWindowViewModel();
        
        var viewModel = (ExtendibleHashFileDebugWindowViewModel)DataContext!;
        viewModel.EhfDebug = ehfDebug;
        
        _openedStandalone = false;
    }
    
    private void Window_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_openedStandalone) 
        {
            OpenExtendibleHashFile();
        }
    }
    
    public void OpenExtendibleHashFile()
    {
        _ehf = new ExtendibleHashFile<Person>(_fileName, _blockSize);
        var viewModel = (ExtendibleHashFileDebugWindowViewModel)DataContext!;
        viewModel.EhfDebug = _ehf.GetDebugObject();
        
        _ehf.Close();
    }

    private void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_ehf != null)
        {
            OpenExtendibleHashFile();
        }
    }
}
