using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Omniroute.Protocol;

/// <summary>
/// JSON формат MQTT повідомлень для Delta 2 і подібних протоколів
/// </summary>
public static class JsonMessages
{
    /// <summary>
    /// Ідентифікатор повідомлення у форматі офіційного застосунку
    /// </summary>
    private static string Seq() => (999_900_000 + Random.Shared.Next(10_000, 99_999)).ToString();

    /// <summary>
    /// Protobuf кадри починаються з 0x0A, тож JSON визначаємо за першим байтом '{'
    /// </summary>
    public static bool LooksLikeJson(byte[] payload) =>
        payload.Length > 0 && payload[0] == (byte)'{';

    /// <summary>
    /// Розпарсити JSON payload в параметри.
    /// Телеметрія: {"params": {"pd.soc": 80, ...}}
    /// Відповідь на запит стану: {"operateType": "latestQuotas", "data": {"online": 1, "quotaMap": {...}}}
    /// </summary>
    public static DeviceParams Parse(byte[] payload)
    {
        var result = new DeviceParams();
        if (!LooksLikeJson(payload))
            return result;

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return result;

            if (root.TryGetProperty("operateType", out var op) &&
                op.ValueKind == JsonValueKind.String &&
                op.GetString() == "latestQuotas")
            {
                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                    return result;
                if (!data.TryGetProperty("online", out var online) ||
                    online.ValueKind != JsonValueKind.Number || online.GetInt32() != 1)
                    return result;
                if (data.TryGetProperty("quotaMap", out var quotaMap))
                    Flatten(quotaMap, "", result);
                return result;
            }

            if (root.TryGetProperty("params", out var parameters))
                Flatten(parameters, "", result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse JSON: {ex.Message}");
        }

        return result;
    }

    private static void Flatten(JsonElement element, string prefix, DeviceParams result)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                Flatten(property.Value, key, result);
            }
            else
            {
                result[key] = GetValue(property.Value);
            }
        }
    }

    private static object? GetValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => ParseArray(element),
            _ => null
        };
    }

    private static List<object?> ParseArray(JsonElement element)
    {
        var list = new List<object?>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(GetValue(item));
        }
        return list;
    }

    /// <summary>
    /// Створити запит останніх квот (повний стан)
    /// </summary>
    public static OutgoingMessage LatestQuotas()
    {
        return Command(0, "latestQuotas", new Dictionary<string, object>(), version: "1.1");
    }

    /// <summary>
    /// Створити команду керування
    /// </summary>
    public static OutgoingMessage Command(
        int moduleType,
        string operateType,
        Dictionary<string, object> parameters,
        string? moduleSn = null,
        string version = "1.0")
    {
        var payload = new Dictionary<string, object>
        {
            ["from"] = "Android",
            ["id"] = Seq(),
            ["version"] = version,
            ["moduleType"] = moduleType,
            ["operateType"] = operateType,
            ["params"] = parameters
        };

        if (!string.IsNullOrEmpty(moduleSn))
        {
            payload["moduleSn"] = moduleSn;
        }

        var json = JsonSerializer.Serialize(payload);
        return new OutgoingMessage(Encoding.UTF8.GetBytes(json));
    }
}
