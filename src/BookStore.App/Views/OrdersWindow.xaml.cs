using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BookStore.App.Helpers;
using BookStore.Data.Models;
using BookStore.Data.Repositories;

namespace BookStore.App.Views;

public partial class OrdersWindow : Window
{
    private readonly OrderRepository _orderRepository = new();

    public OrdersWindow()
    {
        InitializeComponent();

        UserNameText.Text = AppSession.DisplayName;
        UserRoleText.Text = AppSession.CurrentUser?.RoleName ?? string.Empty;
        // Менеджер только просматривает заказы, изменять их может администратор.
        AddOrderButton.Visibility = AppSession.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
        if (AppSession.IsAdministrator)
        {
            OrdersList.ToolTip = "Нажмите на заказ, чтобы отредактировать его";
        }

        LoadOrders();
    }

    private void LoadOrders()
    {
        try
        {
            var orders = _orderRepository.GetAll();
            OrdersList.ItemsSource = orders;
            CountText.Text = $"Всего заказов: {orders.Count}";
        }
        catch (Exception ex)
        {
            OrdersList.ItemsSource = null;
            MessageHelper.ShowError($"Не удалось загрузить список заказов.\n\nПодробности: {ex.Message}");
        }
    }

    private void OrderItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (AppSession.IsAdministrator && sender is ListBoxItem { DataContext: Order order })
        {
            OpenEditWindow(order.OrderId);
        }
    }

    private void AddOrderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenEditWindow(null);
    }

    private void OpenEditWindow(int? orderId)
    {
        if (OrderEditWindow.OpenedWindow != null)
        {
            MessageHelper.ShowWarning(
                "Окно редактирования заказа уже открыто.\n\nСохраните или закройте его, прежде чем открывать другой заказ.");
            OrderEditWindow.OpenedWindow.Activate();
            return;
        }

        OrderEditWindow window;
        try
        {
            window = new OrderEditWindow(orderId) { Owner = this };
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError($"Не удалось открыть карточку заказа.\n\nПодробности: {ex.Message}");
            LoadOrders();
            return;
        }

        window.Closed += (_, _) => LoadOrders();
        window.Show();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        new ProductsWindow().Show();
        Close();
    }
}
