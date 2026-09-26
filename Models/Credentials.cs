namespace Omniroute.Models;

/// <summary>
/// Облікові дані користувача EcoFlow
/// </summary>
public class Credentials
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public DateTime? TokenExpiry { get; set; }

    // MQTT облікові дані
    public string? MqttUsername { get; set; }
    public string? MqttPassword { get; set; }
    public string? MqttUrl { get; set; }
    public int MqttPort { get; set; } = 8883;
}
