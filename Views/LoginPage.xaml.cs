using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Omniroute.Api;
using Omniroute.Models;

namespace Omniroute.Views;

public sealed partial class LoginPage : Page
{
    private readonly EcoflowCloud _api;

    public LoginPage()
    {
        InitializeComponent();
        _api = new EcoflowCloud();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = false;
        ProgressRing.IsActive = true;

        var email = EmailBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Введіть email та пароль");
            return;
        }

        try
        {
            // Спробувати різні регіональні сервери
            var hosts = new[] { "api.ecoflow.com", "api-e.ecoflow.com", "api-a.ecoflow.com" };
            Session? session = null;
            MqttCredentials? mqttCreds = null;
            string? successHost = null;

            foreach (var host in hosts)
            {
                try
                {
                    session = await _api.LoginAsync(host, email, password);
                    mqttCreds = await _api.GetMqttCredentialsAsync(host, session);
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

            if (session == null || mqttCreds == null)
            {
                ShowError("Не вдалося підключитися до серверів EcoFlow");
                return;
            }

            // Зберегти облікові дані
            var credentials = new Credentials
            {
                Email = email,
                Password = password,
                Token = session.Token,
                UserId = session.UserId,
                TokenExpiry = DateTime.Now.AddHours(24),
                MqttUsername = mqttCreds.Username,
                MqttPassword = mqttCreds.Password,
                MqttUrl = mqttCreds.Host,
                MqttPort = mqttCreds.Port
            };

            App.Repository.SaveCredentials(credentials);

            // Перейти до списку пристроїв
            Frame.Navigate(typeof(DevicesPage));
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
