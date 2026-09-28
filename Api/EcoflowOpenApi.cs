using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omniroute.Api;

/// <summary>
/// Пристрій з EcoFlow Cloud
/// </summary>
public record CloudDevice(string Sn, string Name, string? ProductName, bool Online);

/// <summary>
/// Офіційний EcoFlow Developer API (developer.ecoflow.com)
/// Використовується для отримання списку станцій прив'язаних до акаунту
/// Запити підписуються через HMAC-SHA256
/// </summary>
public class EcoflowOpenApi : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;

    public EcoflowOpenApi(HttpClient? httpClient = null)
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
    /// Отримати список пристроїв з акаунту
    /// Автоматично пробує різні регіональні сервери
    /// </summary>
    public async Task<List<CloudDevice>> ListDevicesAsync(string accessKey, string secretKey, string preferredHost = "api.ecoflow.com")
    {
        var hosts = new[] { preferredHost, "api-e.ecoflow.com", "api.ecoflow.com", "api-a.ecoflow.com" }
            .Distinct()
            .ToList();

        EcoflowException? lastError = null;

        foreach (var host in hosts)
        {
            try
            {
                return await FetchListAsync(host, accessKey, secretKey);
            }
            catch (EcoflowException e)
            {
                lastError = e;
                if (!e.IsAuthError)
                    throw;
            }
        }

        throw lastError ?? new EcoflowException("Немає доступних серверів");
    }

    private async Task<List<CloudDevice>> FetchListAsync(string host, string accessKey, string secretKey)
    {
        var nonce = Random.Shared.Next(100_000, 999_999).ToString();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var sign = ComputeHmacSha256(secretKey, $"accessKey={accessKey}&nonce={nonce}&timestamp={timestamp}");

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/iot-open/sign/device/list");
        request.Headers.Add("accessKey", accessKey);
        request.Headers.Add("nonce", nonce);
        request.Headers.Add("timestamp", timestamp);
        request.Headers.Add("sign", sign);

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new EcoflowException($"HTTP {(int)response.StatusCode}");

        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        // code і online можуть прийти як рядком, так і числом
        var code = root.TryGetProperty("code", out var c) ? c.ToString() : null;
        if (code != "0")
        {
            var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : null;
            var errorMsg = string.IsNullOrWhiteSpace(message) ? $"Помилка {code}" : message;

            // 8513: невірний ключ або ключ з іншого регіону
            // 8521: невірний підпис (неправильний секретний ключ)
            var isAuthError = code is "8513" or "8521";
            throw new EcoflowException(errorMsg, isAuthError);
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return new List<CloudDevice>();

        var devices = new List<CloudDevice>();
        foreach (var item in data.EnumerateArray())
        {
            var sn = item.GetProperty("sn").GetString()!;
            var deviceName = item.TryGetProperty("deviceName", out var dn) && dn.ValueKind == JsonValueKind.String ? dn.GetString()?.Trim() : null;
            var name = string.IsNullOrWhiteSpace(deviceName) ? sn : deviceName;
            var productName = item.TryGetProperty("productName", out var pn) && pn.ValueKind == JsonValueKind.String ? pn.GetString() : null;
            var online = item.TryGetProperty("online", out var o) && o.ToString() == "1";

            devices.Add(new CloudDevice(sn, name, productName, online));
        }

        return devices;
    }

    /// <summary>
    /// Обчислити HMAC-SHA256 підпис
    /// </summary>
    private static string ComputeHmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _httpClient?.Dispose();
        }
    }
}
