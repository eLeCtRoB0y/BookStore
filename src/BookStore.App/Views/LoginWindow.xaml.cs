using System.Windows;
using BookStore.App.Helpers;
using BookStore.Data.Models;
using BookStore.Data.Repositories;

namespace BookStore.App.Views;

public partial class LoginWindow : Window
{
    private readonly UserRepository _userRepository = new();

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoginBox.Focus();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var login = LoginBox.Text.Trim();
        var password = PasswordBox.Password;

        if (login.Length == 0 || password.Length == 0)
        {
            MessageHelper.ShowWarning("Введите логин и пароль.\n\nЕсли у вас нет учётной записи, нажмите «Войти как гость».");
            (login.Length == 0 ? (UIElement)LoginBox : PasswordBox).Focus();
            return;
        }

        try
        {
            var user = _userRepository.Authenticate(login, password);
            if (user == null)
            {
                MessageHelper.ShowError("Неверный логин или пароль.\n\nПроверьте раскладку клавиатуры и Caps Lock и повторите ввод.");
                PasswordBox.Clear();
                PasswordBox.Focus();
                return;
            }

            OpenCatalog(user);
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError($"Не удалось выполнить вход: ошибка обращения к базе данных.\n\nПодробности: {ex.Message}");
        }
    }

    private void GuestButton_Click(object sender, RoutedEventArgs e)
    {
        OpenCatalog(null);
    }

    private void OpenCatalog(User? user)
    {
        AppSession.CurrentUser = user;
        new ProductsWindow().Show();
        Close();
    }
}
