using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omniroute.Api;

/// <summary>
/// Виняток EcoFlow API
/// </summary>
public class EcoflowException : Exception
{
    public bool IsAuthError { get; }

    public EcoflowException(string message, bool isAuthError = false) : base(message)
    {
        IsAuthError = isAuthError;
    }
}

/// <summary>
/// Сесія користувача EcoFlow
/// </summary>
public record Session(string Token, string UserId, string UserName);

/// <summary>
/// Облікові дані MQTT
/// </summary>
public record MqttCredentials(string Host, int Port, string Username, string Password);

/// <summary>
/// REST API клієнт EcoFlow Cloud (неофіційний)
/// Використовує ті самі ендпоінти, що і офіційний мобільний застосунок
/// </summary>
public class EcoflowCloud : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;

    public EcoflowCloud(HttpClient? httpClient = null)
    {
        if (httpClient == null)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            _ownsClient = true;
        }
        else
        {
            _httpClient = httpClient;
            _ownsClient = false;
        }
    }

    /// <summary>
    /// Увійти в акаунт EcoFlow
    /// </summary>
    /// <param name="host">Регіональний сервер (api.ecoflow.com, api-e.ecoflow.com тощо)</param>
    /// <param name="email">Email користувача</param>
    /// <param name="password">Пароль</param>
    /// <returns>Сесія з токеном</returns>
    public async Task<Session> LoginAsync(string host, string email, string password)
    {
        var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));

        var requestBody = new
        {
            email,
            password = encodedPassword,
            scene = "IOT_APP",
            userType = "ECOFLOW"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/auth/login")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            )
        };
        request.Headers.Add("lang", "en_US");

        var data = await ExecuteAsync(request, authCall: true);

        if (!data.TryGetProperty("user", out var user))
            throw new EcoflowException("Сервер не повернув дані користувача");

        return new Session(
            Token: data.GetProperty("token").GetString()!,
            UserId: user.GetProperty("userId").ToString(),
            UserName: user.TryGetProperty("name", out var name) ? name.GetString() ?? "" : ""
        );
    }

    /// <summary>
    /// Отримати облікові дані для підключення до MQTT брокера
    /// </summary>
    public async Task<MqttCredentials> GetMqttCredentialsAsync(string host, Session session)
    {
        var url = $"https://{host}/iot-auth/app/certification?userId={Uri.EscapeDataString(session.UserId)}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("lang", "en_US");
        request.Headers.Add("authorization", $"Bearer {session.Token}");

        var data = await ExecuteAsync(request, authCall: false);

        return new MqttCredentials(
            Host: data.GetProperty("url").GetString()!,
            Port: data.GetProperty("port").GetInt32(),
            Username: data.GetProperty("certificateAccount").GetString()!,
            Password: data.GetProperty("certificatePassword").GetString()!
        );
    }

    private async Task<JsonElement> ExecuteAsync(HttpRequestMessage request, bool authCall)
    {
        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new EcoflowException($"HTTP {(int)response.StatusCode}");

        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : null;
        if (!string.Equals(message, "success", StringComparison.OrdinalIgnoreCase))
        {
            var errorMsg = string.IsNullOrWhiteSpace(message)
                ? $"Помилка {root.GetProperty("code")}"
                : message;
            throw new EcoflowException(errorMsg, isAuthError: authCall);
        }

        if (!root.TryGetProperty("data", out var data))
            throw new EcoflowException("Порожня відповідь сервера");

        return data;
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _httpClient?.Dispose();
        }
    }
}
