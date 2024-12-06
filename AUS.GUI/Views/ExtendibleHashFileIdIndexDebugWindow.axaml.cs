using System;
using AUS.DataStructures.CarService;
using AUS.DataStructures.ExtendibleHashFile;
using AUS.GUI.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AUS.GUI.Views;

public partial class ExtendibleHashFileIdIndexDebugWindow : Window
{
    private readonly Func<EhfDebug<KeyToBlockAddress<PersonIdKey>>>? _getEhfDebug;
    
    public ExtendibleHashFileIdIndexDebugWindow()
    {
        InitializeComponent();
        DataContext = new ExtendibleHashFileIdIndexDebugWindowViewModel();
    }
    
    public ExtendibleHashFileIdIndexDebugWindow(Func<EhfDebug<KeyToBlockAddress<PersonIdKey>>> getEhfDebug)
    {
        InitializeComponent();
        DataContext = new ExtendibleHashFileIdIndexDebugWindowViewModel();
        
        var viewModel = (ExtendibleHashFileIdIndexDebugWindowViewModel)DataContext!;
        viewModel.EhfDebug = getEhfDebug();
        
        _getEhfDebug = getEhfDebug;
    }

    private void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = (ExtendibleHashFileIdIndexDebugWindowViewModel)DataContext!;
        viewModel.EhfDebug = _getEhfDebug!();
    }
}
