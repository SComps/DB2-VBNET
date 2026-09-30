using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Db2Spufi.Avalonia;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
