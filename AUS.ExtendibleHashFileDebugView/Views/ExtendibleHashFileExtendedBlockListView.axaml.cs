using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AUS.ExtendibleHashFileDebugView.Views;

public partial class ExtendibleHashFileExtendedBlockListView : UserControl
{
    public ExtendibleHashFileExtendedBlockListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
