using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Omniroute.Api;
using Omniroute.Models;
using Omniroute.Services;

namespace Omniroute.Views;

public sealed partial class LoginPage : Page
{
    private readonly EcoflowCloud _api;

    public LoginPage()
    {
        InitializeComponent();
        _api = new EcoflowCloud();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _api.Dispose();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var email = EmailBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Введіть email та пароль");
            return;
        }

        LoginButton.IsEnabled = false;
        ProgressRing.IsActive = true;

        try
        {
            // Спробувати різні регіональні сервери
            var hosts = new[] { "api.ecoflow.com", "api-e.ecoflow.com", "api-a.ecoflow.com" };
            string? successHost = null;

            foreach (var host in hosts)
            {
                try
                {
                    var session = await _api.LoginAsync(host, email, password);
                    // Перевірити, що з цього сервера видаються облікові дані MQTT
                    await _api.GetMqttCredentialsAsync(host, session);
                    successHost = host;
                    break;
                }
                catch (EcoflowException ex) when (ex.IsAuthError)
                {
                    throw; // Помилка авторизації - не пробувати інші сервери
                }
                catch
                {
                    continue; // Спробувати наступний сервер
                }
            }

            if (successHost == null)
            {
                ShowError("Не вдалося підключитися до серверів EcoFlow");
                return;
            }

            // Зберегти облікові дані (ключі Developer API зберігаються, якщо вхід у той самий акаунт)
            var previous = App.Repository.Credentials;
            var sameAccount = string.Equals(previous?.Email, email, StringComparison.OrdinalIgnoreCase);
            App.Repository.SaveCredentials(new Credentials
            {
                Email = email,
                Password = password,
                ApiHost = successHost,
                AccessKey = sameAccount ? previous?.AccessKey : null,
                SecretKey = sameAccount ? previous?.SecretKey : null
            });

            // Запустити моніторинг з новими обліковими даними
            MonitorService.Stop();
            MonitorService.Start();

            // Перейти до списку пристроїв
            Frame.Navigate(typeof(DevicesPage));
            Frame.BackStack.Clear();
        }
        catch (EcoflowException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Помилка: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            ProgressRing.IsActive = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
        LoginButton.IsEnabled = true;
        ProgressRing.IsActive = false;
    }
}
