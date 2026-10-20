using System.Windows;

namespace BookStore.App.Helpers;

/// <summary>Окна сообщений с заголовком и пиктограммой, соответствующими типу сообщения.</summary>
public static class MessageHelper
{
    public static void ShowError(string text) =>
        MessageBox.Show(text, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

    public static void ShowWarning(string text) =>
        MessageBox.Show(text, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void ShowInfo(string text) =>
        MessageBox.Show(text, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

    public static bool Confirm(string text) =>
        MessageBox.Show(text, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
}
