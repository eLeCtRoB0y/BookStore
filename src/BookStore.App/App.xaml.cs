using System.IO;
using System.Windows;
using System.Windows.Threading;
using BookStore.App.Helpers;
using BookStore.App.Views;
using BookStore.Data;

namespace BookStore.App;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            Database.EnsureCreated();
            Directory.CreateDirectory(ImageHelper.ImagesDirectory);
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError(
                "Не удалось подключиться к базе данных.\n\n" +
                $"Убедитесь, что файл BookStore.db (или скрипт BookStore.sql) находится в папке программы:\n{AppContext.BaseDirectory}\n\n" +
                $"Подробности: {ex.Message}");
            Shutdown();
            return;
        }

        new LoginWindow().Show();
    }

    // Последний рубеж: необработанная ошибка показывается пользователю, а приложение не падает.
    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageHelper.ShowError(
            "Произошла непредвиденная ошибка. Текущее действие не выполнено, попробуйте повторить его.\n\n" +
            $"Подробности: {e.Exception.Message}");
        e.Handled = true;
    }
}
