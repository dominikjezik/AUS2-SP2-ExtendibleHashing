using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AUS.HeapFileDebugView.Views;

public partial class HeapFileBlockListView : UserControl
{
    public HeapFileBlockListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
