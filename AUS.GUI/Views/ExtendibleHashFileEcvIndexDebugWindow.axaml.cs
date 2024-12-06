using System;
using AUS.DataStructures.CarService;
using AUS.DataStructures.ExtendibleHashFile;
using AUS.GUI.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AUS.GUI.Views;

public partial class ExtendibleHashFileEcvIndexDebugWindow : Window
{
    private readonly Func<EhfDebug<KeyToBlockAddress<EcvKey>>>? _getEhfDebug;
    
    public ExtendibleHashFileEcvIndexDebugWindow()
    {
        InitializeComponent();
        DataContext = new ExtendibleHashFileEcvIndexDebugWindowViewModel();
    }
    
    public ExtendibleHashFileEcvIndexDebugWindow(Func<EhfDebug<KeyToBlockAddress<EcvKey>>> getEhfDebug)
    {
        InitializeComponent();
        DataContext = new ExtendibleHashFileEcvIndexDebugWindowViewModel();
        
        var viewModel = (ExtendibleHashFileEcvIndexDebugWindowViewModel)DataContext!;
        viewModel.EhfDebug = getEhfDebug();
        
        _getEhfDebug = getEhfDebug;
    }

    private void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = (ExtendibleHashFileEcvIndexDebugWindowViewModel)DataContext!;
        viewModel.EhfDebug = _getEhfDebug!();
    }
}
