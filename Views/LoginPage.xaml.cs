using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Omniroute.Views;

public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var email = EmailBox.Text;
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorText.Text = "Введіть email та пароль";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        // TODO: Реалізувати вхід через EcoFlow API
        Frame.Navigate(typeof(DevicesPage));
    }
}
