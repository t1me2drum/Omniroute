using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Omniroute.Models;

namespace Omniroute.Views;

/// <summary>
/// Спільні кольори й діалоги сторінок
/// </summary>
public static class Ui
{
    /// <summary>Бурштиновий — «мережа є, але заслабка для заряду»</summary>
    public static readonly SolidColorBrush WarningBrush = new(ColorHelper.FromArgb(0xFF, 0xF2, 0xA9, 0x00));

    public static readonly SolidColorBrush SuccessBrush = new(ColorHelper.FromArgb(0xFF, 0x0E, 0x9F, 0x82));

    public static readonly SolidColorBrush ErrorBrush = new(ColorHelper.FromArgb(0xFF, 0xE8, 0x3B, 0x3B));

    /// <summary>
    /// Колір попередження про мережу для x:Bind: 1 — слабка, 2 — немає
    /// </summary>
    public static Brush AlertBrush(int gridLevel) => gridLevel == 1 ? WarningBrush : ErrorBrush;

    public static Visibility VisibleIf(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static async Task ShowMessageAsync(XamlRoot root, string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = "OK",
            XamlRoot = root
        };
        await dialog.ShowAsync();
    }

    /// <summary>
    /// Діалог перейменування; повертає нову назву або null
    /// </summary>
    public static async Task<string?> AskNameAsync(XamlRoot root, string current)
    {
        var box = new TextBox { Header = "Назва", Text = current };
        box.SelectAll();
        var dialog = new ContentDialog
        {
            Title = "Назва станції",
            Content = box,
            PrimaryButtonText = "Зберегти",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = root
        };
        box.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(box.Text);

        return await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(box.Text)
            ? box.Text.Trim()
            : null;
    }

    /// <summary>
    /// Підтвердження видалення станції (як DeleteStationDialog в Android-версії)
    /// </summary>
    public static async Task<bool> ConfirmDeleteAsync(XamlRoot root, Device device)
    {
        var text = device.IsImported
            ? "Станцію буде прибрано лише з Omniroute на цьому комп'ютері. В акаунті EcoFlow і в офіційному " +
              "застосунку вона лишиться. Під час синхронізації вона не повернеться; відновити можна в Налаштуваннях."
            : "Станцію буде прибрано лише з Omniroute на цьому комп'ютері. В акаунті EcoFlow вона лишиться.";

        var dialog = new ContentDialog
        {
            Title = $"Видалити {device.Name}?",
            Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = "Видалити",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = root
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Текст результату синхронізації (як describeSync в Android-версії)
    /// </summary>
    public static string DescribeSync(Data.SyncResult r)
    {
        var text = $"Синхронізовано: нових {r.Added}, оновлено {r.Updated}, прибрано {r.Removed}.";
        if (r.Unsupported.Count > 0)
            text += $" Поки не підтримуються: {string.Join(", ", r.Unsupported)}.";
        return text;
    }
}
