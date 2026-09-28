using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Omniroute.Models;

namespace Omniroute.Data;

/// <summary>
/// Зберігання облікових даних користувача (шифрування DPAPI для поточного користувача Windows)
/// </summary>
public class CredentialStore
{
    private const string CredentialsKey = "user_credentials";
    private readonly LocalStore _store = LocalStore.Default;

    /// <summary>
    /// Зберегти облікові дані
    /// </summary>
    public void SaveCredentials(Credentials credentials)
    {
        var json = JsonSerializer.Serialize(credentials);
        _store.Set(CredentialsKey, ProtectData(json));
    }

    /// <summary>
    /// Завантажити облікові дані
    /// </summary>
    public Credentials? LoadCredentials()
    {
        if (_store.Get(CredentialsKey) is string encrypted)
        {
            try
            {
                var credentials = JsonSerializer.Deserialize<Credentials>(UnprotectData(encrypted));
                return string.IsNullOrEmpty(credentials?.Email) ? null : credentials;
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
        _store.Remove(CredentialsKey);
    }

    private static string ProtectData(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string UnprotectData(string encryptedData)
    {
        var bytes = Convert.FromBase64String(encryptedData);
        var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(unprotected);
    }
}
