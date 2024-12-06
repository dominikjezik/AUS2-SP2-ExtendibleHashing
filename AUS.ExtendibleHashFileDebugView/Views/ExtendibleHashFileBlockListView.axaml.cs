using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AUS.ExtendibleHashFileDebugView.Views;

public partial class ExtendibleHashFileBlockListView : UserControl
{
    public ExtendibleHashFileBlockListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
