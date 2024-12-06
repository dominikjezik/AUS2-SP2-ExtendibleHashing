using System;
using AUS.DataStructures.CarService;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AUS.GUI.Views;

public partial class GenerateObjectsWindow : Window
{
    public event EventHandler<GenerateOptions>? GenerateObjects;

    public GenerateOptions Options { get; set; } = new();
    
    public GenerateObjectsWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void GenerateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GenerateObjects?.Invoke(this, Options);
        Close();
    }
}
