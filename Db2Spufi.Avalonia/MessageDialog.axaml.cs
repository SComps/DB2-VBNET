using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Db2Spufi.Avalonia;

public partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    public static async Task ShowAsync(Window owner, string message, string title = "Information")
    {
        var dlg = new MessageDialog
        {
            Title = title
        };
        dlg.TxtMessage.Text = message;
        await dlg.ShowDialog(owner);
    }

    public static async Task<bool> ShowConfirmAsync(Window owner, string message, string title = "Confirm")
    {
        var dlg = new MessageDialog
        {
            Title = title
        };
        dlg.TxtMessage.Text = message;
        dlg.BtnCancel.IsVisible = true;
        var result = await dlg.ShowDialog<bool>(owner);
        return result;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
