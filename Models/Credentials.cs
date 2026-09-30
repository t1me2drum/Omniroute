using System.Text.Json.Serialization;

namespace PowerHub.Models;

/// <summary>
/// Облікові дані користувача EcoFlow (зберігаються зашифрованими).
/// Токен і облікові дані MQTT не зберігаються: їх отримуємо заново при кожному підключенні.
/// </summary>
public class Credentials
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Регіональний сервер, на якому вдався вхід
    /// </summary>
    public string ApiHost { get; set; } = "api.ecoflow.com";

    // Ключі EcoFlow Developer API (developer.ecoflow.com)
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }

    [JsonIgnore]
    public bool HasDeveloperKeys => !string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(SecretKey);
}
