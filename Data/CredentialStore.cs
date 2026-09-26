using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Omniroute.Models;
using Windows.Storage;

namespace Omniroute.Data;

/// <summary>
/// Зберігання облікових даних користувача
/// </summary>
public class CredentialStore
{
    private const string CredentialsKey = "user_credentials";
    private readonly ApplicationDataContainer _settings;

    public CredentialStore()
    {
        _settings = ApplicationData.Current.LocalSettings;
    }

    /// <summary>
    /// Зберегти облікові дані
    /// </summary>
    public void SaveCredentials(Credentials credentials)
    {
        var json = JsonSerializer.Serialize(credentials);
        var encrypted = ProtectData(json);
        _settings.Values[CredentialsKey] = encrypted;
    }

    /// <summary>
    /// Завантажити облікові дані
    /// </summary>
    public Credentials? LoadCredentials()
    {
        if (_settings.Values.TryGetValue(CredentialsKey, out var value) && value is string encrypted)
        {
            try
            {
                var json = UnprotectData(encrypted);
                return JsonSerializer.Deserialize<Credentials>(json);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Видалити облікові дані
    /// </summary>
    public void ClearCredentials()
    {
        _settings.Values.Remove(CredentialsKey);
    }

    /// <summary>
    /// Захист даних (шифрування)
    /// </summary>
    private string ProtectData(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var protected = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protected);
    }

    /// <summary>
    /// Розшифрування даних
    /// </summary>
    private string UnprotectData(string encryptedData)
    {
        var bytes = Convert.FromBase64String(encryptedData);
        var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(unprotected);
    }
}
