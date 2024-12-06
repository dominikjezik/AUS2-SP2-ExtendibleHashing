using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AUS.HeapFileDebugView.Views;

public partial class HeapFileExtendedBlockListView : UserControl
{
    public HeapFileExtendedBlockListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
