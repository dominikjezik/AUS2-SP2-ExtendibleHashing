using System;
using AUS.DataStructures.CarService;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AUS.GUI.Views;

public partial class CreatePersonWindow : Window
{
    public PersonDTO Person { get; private set; } = new();
    
    public event EventHandler<PersonDTO>? CreatePerson;
    
    public CreatePersonWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
        CreatePerson?.Invoke(this, Person);
    }
}
