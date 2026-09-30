using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Omniroute.Services;

/// <summary>
/// Журнал діагностики, яким користувач може поділитися, коли щось працює не так
/// (як DiagLog в Android-версії): зміни з'єднання, збої розбору даних, невдалі команди й падіння.
/// Файл: %LocalAppData%\Omniroute\diag.log. Паролі, токени, ключі й облікові дані MQTT сюди не передавати.
/// </summary>
public static class DiagLog
{
    private const int MaxBytes = 256 * 1024;
    private const int MaxStackLines = 40;

    private static readonly object _lock = new();

    public static readonly string FilePath = Path.Combine(Data.LocalStore.AppFolder, "diag.log");

    public static void Log(string tag, string message, Exception? error = null)
    {
        System.Diagnostics.Debug.WriteLine($"[{tag}] {message}{(error != null ? $": {error.Message}" : "")}");

        var entry = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            .Append(" [").Append(tag).Append("] ").Append(message).Append('\n');
        if (error != null)
        {
            foreach (var line in error.ToString().Split('\n').Take(MaxStackLines))
                entry.Append(line.TrimEnd('\r')).Append('\n');
        }

        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(Data.LocalStore.AppFolder);
                var info = new FileInfo(FilePath);
                if (info.Exists && info.Length > MaxBytes)
                {
                    // Лишаємо новішу половину, щоб журнал не ріс без меж
                    var text = File.ReadAllText(FilePath);
                    var half = text[(text.Length / 2)..];
                    var newline = half.IndexOf('\n');
                    File.WriteAllText(FilePath, newline >= 0 ? half[(newline + 1)..] : half);
                }
                File.AppendAllText(FilePath, entry.ToString());
            }
            catch
            {
                // Нема куди повідомити про помилку запису журналу
            }
        }
    }

    public static string Read()
    {
        lock (_lock)
        {
            try
            {
                return File.Exists(FilePath) ? File.ReadAllText(FilePath) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            try
            {
                File.WriteAllText(FilePath, string.Empty);
            }
            catch
            {
                // Файл зайнятий або недоступний — лишаємо як є
            }
        }
    }

    public static int LineCount() => Read().Count(c => c == '\n');

    /// <summary>
    /// Журнал із заголовком (версія застосунку й Windows) — для копіювання в буфер обміну
    /// </summary>
    public static string ExportText()
    {
        var version = typeof(DiagLog).Assembly.GetName().Version?.ToString(3) ?? "?";
        return $"Omniroute {version} · {Environment.OSVersion.VersionString} · {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}\n\n{Read()}";
    }

    /// <summary>
    /// Початок кадру у hex, щоб за журналом можна було впізнати невідомий формат
    /// </summary>
    public static string Hex(byte[] payload, int maxBytes = 48) =>
        Convert.ToHexString(payload, 0, Math.Min(payload.Length, maxBytes)).ToLowerInvariant();
}
