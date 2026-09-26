using Microsoft.UI.Xaml.Controls;

namespace Omniroute.Views;

public sealed partial class DevicesPage : Page
{
    public DevicesPage()
    {
        InitializeComponent();
        LoadDevices();
    }

    private void LoadDevices()
    {
        // TODO: Завантажити список станцій з Repository
    }
}
