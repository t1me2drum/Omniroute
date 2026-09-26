using System.Text;
using System.Text.Json;

namespace Omniroute.Protocol;

/// <summary>
/// Парсер JSON повідомлень для Delta 2 і подібних протоколів
/// </summary>
public static class JsonMessages
{
    /// <summary>
    /// Розпарсити JSON payload в параметри
    /// </summary>
    public static DeviceParams Parse(byte[] payload)
    {
        var result = new DeviceParams();

        try
        {
            var json = Encoding.UTF8.GetString(payload);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Рекурсивно обійти всі поля з префіксом
            ParseObject(root, "", result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse JSON: {ex.Message}");
        }

        return result;
    }

    private static void ParseObject(JsonElement element, string prefix, DeviceParams result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    ParseObject(property.Value, key, result);
                }
                else
                {
                    result[key] = GetValue(property.Value);
                }
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
            JsonValueKind.Null => null,
            _ => null
        };
    }

    /// <summary>
    /// Створити запит останніх квот (повний стан)
    /// </summary>
    public static OutgoingMessage LatestQuotas()
    {
        var json = JsonSerializer.Serialize(new
        {
            from = "iOS",
            lang = "en-us",
            id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            moduleType = 0,
            operateType = "latestQuotas",
            version = "1.0"
        });

        return new OutgoingMessage(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Створити команду керування
    /// </summary>
    public static OutgoingMessage Command(int moduleType, string operateType, Dictionary<string, object> parameters, string? moduleSn = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["from"] = "iOS",
            ["lang"] = "en-us",
            ["id"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            ["moduleType"] = moduleType,
            ["operateType"] = operateType,
            ["version"] = "1.0",
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
