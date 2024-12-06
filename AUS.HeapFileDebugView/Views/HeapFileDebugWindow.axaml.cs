using System;
using AUS.DataStructures.CarService;
using AUS.DataStructures.HeapFile;
using AUS.HeapFileDebugView.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AUS.HeapFileDebugView.Views;

public partial class HeapFileDebugWindow : Window
{
    private const int DefaultBlockSize = 28000;
    //private const string DefaultFileName = @"C:\Users\dominik\Desktop\heapFile.dat";
    //private const string DefaultFileName = @"C:\Users\dominik\Desktop\TEST.dat";
    //private const string DefaultFileName = "/Users/dominik/Desktop/heapFile.dat";
    private const string DefaultFileName = "/Users/dominik/Desktop/TEST.dat";
    
    private readonly string _fileName = DefaultFileName;
    private readonly int _blockSize = DefaultBlockSize;
    
    private bool _openedStandalone = true;
    private HeapFile<Person>? _heapFile;
    private readonly Func<HfDebug<Person>>? _getHfDebug;

    public HeapFileDebugWindow()
    {
        InitializeComponent();
        DataContext = new HeapFileDebugWindowViewModel();
    }
    
    public HeapFileDebugWindow(int blockSize, string fileName)
    {
        InitializeComponent();
        DataContext = new HeapFileDebugWindowViewModel();
        
        _blockSize = blockSize;
        _fileName = fileName;
    }
    
    public HeapFileDebugWindow(Func<HfDebug<Person>> getHfDebug)
    {
        InitializeComponent();
        DataContext = new HeapFileDebugWindowViewModel();
        
        var viewModel = (HeapFileDebugWindowViewModel)DataContext!;
        viewModel.HfDebug = getHfDebug();
        
        _openedStandalone = false;
        _getHfDebug = getHfDebug;
    }
    
    private void Window_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_openedStandalone) 
        {
            OpenHeapFile();
        }
    }
    
    public void OpenHeapFile()
    {
        _heapFile = new HeapFile<Person>(_fileName, _blockSize);
        var viewModel = (HeapFileDebugWindowViewModel)DataContext!;
        viewModel.HfDebug = _heapFile.GetDebugObject();

        _heapFile.Close();
    }

    private void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_heapFile != null)
        {
            OpenHeapFile();
        }
        else
        {
            var viewModel = (HeapFileDebugWindowViewModel)DataContext!;
            viewModel.HfDebug = _getHfDebug();
        }
    }
}
