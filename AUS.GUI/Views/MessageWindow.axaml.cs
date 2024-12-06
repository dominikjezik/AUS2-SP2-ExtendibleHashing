using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AUS.GUI.Views;

public partial class MessageWindow : Window
{
    public MessageWindow(string message)
    {
        InitializeComponent();
        TextBlockMessage.Text = message;
    }

    private void ButtonOk_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}