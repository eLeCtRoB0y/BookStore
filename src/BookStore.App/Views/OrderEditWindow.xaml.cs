using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookStore.App.Helpers;
using BookStore.Data.Models;
using BookStore.Data.Repositories;
using BookStore.Data.Services;

namespace BookStore.App.Views;

public partial class OrderEditWindow : Window
{
    /// <summary>Открытое окно редактирования заказа (одновременно допускается только одно).</summary>
    public static OrderEditWindow? OpenedWindow { get; private set; }

    private readonly OrderRepository _orderRepository = new();
    private readonly UserRepository _userRepository = new();
    private readonly Order? _order;

    public OrderEditWindow(int? orderId)
    {
        InitializeComponent();

        StatusBox.ItemsSource = LookupRepository.GetOrderStatuses();
        PickupPointBox.ItemsSource = LookupRepository.GetPickupPoints();
        ClientBox.ItemsSource = _userRepository.GetClients();

        if (orderId.HasValue)
        {
            _order = _orderRepository.GetById(orderId.Value)
                     ?? throw new InvalidOperationException("Заказ не найден в базе данных: возможно, он уже удалён.");
        }

        if (_order == null)
        {
            Title = "Добавление заказа";
            HeaderText.Text = "Добавление заказа";
            IdLabel.Visibility = Visibility.Collapsed;
            IdBox.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
            StatusBox.SelectedIndex = 0;
            CodeBox.Text = _orderRepository.GetNextReceiveCode().ToString();
            OrderDatePicker.SelectedDate = DateTime.Today;
            DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(3);
        }
        else
        {
            Title = "Редактирование заказа";
            HeaderText.Text = $"Редактирование заказа №{_order.OrderId}";
            IdBox.Text = _order.OrderId.ToString();
            ArticleBox.Text = _order.ArticleText;
            StatusBox.SelectedValue = _order.OrderStatusId;
            PickupPointBox.SelectedValue = _order.PickupPointId;
            ClientBox.SelectedValue = _order.UserId;
            CodeBox.Text = _order.ReceiveCode.ToString();
            OrderDatePicker.SelectedDate = _order.OrderDate;
            DeliveryDatePicker.SelectedDate = _order.DeliveryDate;
        }

        OpenedWindow = this;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var errors = new List<string>();
        ResetHighlight();

        List<OrderLine> lines;
        string linesError;
        try
        {
            OrderLineParser.TryParse(ArticleBox.Text, _orderRepository.GetProductIdsByArticle(), out lines, out linesError);
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError($"Не удалось проверить артикулы в базе данных.\n\nПодробности: {ex.Message}");
            return;
        }

        Require(linesError.Length == 0, ArticleBox, linesError, errors);
        Require(StatusBox.SelectedValue != null, StatusBox, "Выберите статус заказа.", errors);
        Require(PickupPointBox.SelectedValue != null, PickupPointBox, "Выберите адрес пункта выдачи.", errors);
        Require(ClientBox.SelectedValue != null, ClientBox, "Выберите клиента, оформившего заказ.", errors);

        var codeOk = int.TryParse(CodeBox.Text.Trim(), out var code) && code > 0;
        Require(codeOk, CodeBox, "Код для получения должен быть целым положительным числом.", errors);

        var orderDate = OrderDatePicker.SelectedDate;
        var deliveryDate = DeliveryDatePicker.SelectedDate;
        Require(orderDate.HasValue, OrderDatePicker, "Укажите дату заказа.", errors);
        Require(deliveryDate.HasValue, DeliveryDatePicker, "Укажите дату выдачи.", errors);
        if (orderDate.HasValue && deliveryDate.HasValue)
        {
            Require(deliveryDate.Value.Date >= orderDate.Value.Date, DeliveryDatePicker,
                "Дата выдачи не может быть раньше даты заказа.", errors);
        }

        if (errors.Count > 0)
        {
            MessageHelper.ShowWarning("Заказ не сохранён. Исправьте ошибки (поля выделены красным):\n\n• " +
                                      string.Join("\n• ", errors));
            return;
        }

        var order = _order ?? new Order();
        order.Lines = lines;
        order.OrderStatusId = (int)StatusBox.SelectedValue!;
        order.PickupPointId = (int)PickupPointBox.SelectedValue!;
        order.UserId = (int)ClientBox.SelectedValue!;
        order.ReceiveCode = code;
        order.OrderDate = orderDate!.Value.Date;
        order.DeliveryDate = deliveryDate!.Value.Date;

        try
        {
            if (_order == null)
            {
                _orderRepository.Insert(order);
            }
            else
            {
                _orderRepository.Update(order);
            }
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError($"Не удалось сохранить заказ.\n\nПодробности: {ex.Message}");
            return;
        }

        MessageHelper.ShowInfo(_order == null
            ? $"Заказ №{order.OrderId} добавлен."
            : $"Изменения заказа №{order.OrderId} сохранены.");
        Close();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_order == null)
        {
            return;
        }

        if (!MessageHelper.Confirm($"Удалить заказ №{_order.OrderId}?\n\nЭто действие нельзя отменить."))
        {
            return;
        }

        try
        {
            _orderRepository.Delete(_order.OrderId);
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError($"Не удалось удалить заказ.\n\nПодробности: {ex.Message}");
            return;
        }

        MessageHelper.ShowInfo($"Заказ №{_order.OrderId} удалён.");
        Close();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        OpenedWindow = null;
    }

    private static void Require(bool condition, Control control, string message, List<string> errors)
    {
        if (condition)
        {
            return;
        }

        errors.Add(message);
        control.BorderBrush = Brushes.Red;
        control.BorderThickness = new Thickness(2);
    }

    private void ResetHighlight()
    {
        foreach (Control control in new Control[]
                 {
                     ArticleBox, StatusBox, PickupPointBox, ClientBox, CodeBox, OrderDatePicker, DeliveryDatePicker
                 })
        {
            control.ClearValue(BorderBrushProperty);
            control.ClearValue(BorderThicknessProperty);
        }
    }
}
