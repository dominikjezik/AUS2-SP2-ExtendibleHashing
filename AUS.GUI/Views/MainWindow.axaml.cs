using System;
using AUS.DataStructures.CarService;
using AUS.GUI.ViewModels;
using AUS.HeapFileDebugView.Views;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AUS.GUI.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
    
    private ApplicationService Service => ViewModel.Service;
    
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        Service.Close();
    }

    private void CreatePersonButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var detailsWindow = new CreatePersonWindow();
        detailsWindow.CreatePerson += OnNewPerson;
        detailsWindow.ShowDialog(this);
    }
    
    private void OnNewPerson(object? sender, PersonDTO person)
    {
        try
        {
            Service.Insert(person);
        }
        catch (Exception exception)
        {
            var messageWindow = new MessageWindow(exception.Message);
            messageWindow.ShowDialog(this);
        }
    }

    private void GenerateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var generatePersonsWindow = new GenerateObjectsWindow();
        generatePersonsWindow.GenerateObjects += OnGeneratePersons;
        generatePersonsWindow.ShowDialog(this);
    }
    
    private void OnGeneratePersons(object? sender, GenerateOptions options) => ViewModel.GeneratePersons(options);

    private void SearchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var person = Service.Get(ViewModel.PersonQuery);

        ViewModel.SelectedPerson = person;
        
        if (person == null)
        {
            var messageWindow = new MessageWindow("Osoba sa nenašla.");
            messageWindow.ShowDialog(this);
        }
    }
    
    private void EHFIndexIDButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var debugViewWindow = new ExtendibleHashFileIdIndexDebugWindow(Service.GetDebugIndexByPersonIdEhf);
        debugViewWindow.Show();
    }
    
    private void EHFIndexECVButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var debugViewWindow = new ExtendibleHashFileEcvIndexDebugWindow(Service.GetDebugIndexByEcvEhf);
        debugViewWindow.Show();
    }

    private void HFDataButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var debugViewWindow = new HeapFileDebugWindow(Service.GetDebugDataHeapFile);
        debugViewWindow.Show();
    }

    private void DeletePersonButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.DeleteSelectedPerson();
    }

    private void SavePersonButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.UpdateSelectedPerson();
        }
        catch (Exception exception)
        {
            var messageWindow = new MessageWindow(exception.Message);
            messageWindow.ShowDialog(this);
        }
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = (ServiceVisitDTO)((DataGrid)sender!).SelectedItem;
        ViewModel.SelectedServiceVisit = selected;
    }

    private void CreateServiceVisitButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var insertedServiceVisit = ViewModel.InsertNewServiceVisit();
        ServiceVisitsDataGrid.SelectedItem = insertedServiceVisit;
    }

    private void DeleteServiceVisitButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.DeleteSelectedServiceVisit();
        ServiceVisitsDataGrid.SelectedItem = null;
    }
}
