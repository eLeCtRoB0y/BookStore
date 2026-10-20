using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BookStore.App.Helpers;
using BookStore.Data.Models;
using BookStore.Data.Repositories;
using BookStore.Data.Services;

namespace BookStore.App.Views;

public partial class ProductsWindow : Window
{
    private readonly ProductRepository _productRepository = new();
    private List<Product> _allProducts = new();

    public ProductsWindow()
    {
        InitializeComponent();

        UserNameText.Text = AppSession.DisplayName;
        UserRoleText.Text = AppSession.CurrentUser?.RoleName ?? "Гость";

        // Гость и клиент видят только список; менеджер — ещё поиск/сортировку/фильтр и заказы;
        // администратор — дополнительно добавление и редактирование.
        ToolsPanel.Visibility = AppSession.CanManageCatalog ? Visibility.Visible : Visibility.Collapsed;
        AddProductButton.Visibility = AppSession.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;

        SortBox.SelectedIndex = 0;
        DiscountBox.SelectedIndex = 0;

        LoadProducts();
    }

    private void LoadProducts()
    {
        try
        {
            _allProducts = _productRepository.GetAll();
        }
        catch (Exception ex)
        {
            _allProducts = new List<Product>();
            MessageHelper.ShowError($"Не удалось загрузить список товаров из базы данных.\n\nПодробности: {ex.Message}");
        }

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        // Обработчики могут сработать во время InitializeComponent, когда элементы ещё не созданы.
        if (ProductsList == null || SortBox == null || DiscountBox == null || SearchBox == null)
        {
            return;
        }

        List<Product> visible;
        if (AppSession.CanManageCatalog)
        {
            var range = (DiscountRange)Math.Max(0, DiscountBox.SelectedIndex);
            var sort = (ProductSort)Math.Max(0, SortBox.SelectedIndex);
            visible = ProductFilterService.Apply(_allProducts, SearchBox.Text, range, sort);
        }
        else
        {
            visible = _allProducts;
        }

        ProductsList.ItemsSource = visible;
        CountText.Text = $"Показано товаров: {visible.Count} из {_allProducts.Count}";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        ApplyFilters();
    }

    private void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ProductItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (!AppSession.IsAdministrator)
        {
            return;
        }

        if (sender is ListBoxItem { DataContext: Product product })
        {
            OpenEditWindow(product.ProductId);
        }
    }

    private void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        OpenEditWindow(null);
    }

    private void OpenEditWindow(int? productId)
    {
        // Защита от одновременного редактирования нескольких товаров.
        if (ProductEditWindow.OpenedWindow != null)
        {
            MessageHelper.ShowWarning(
                "Окно редактирования товара уже открыто.\n\nСохраните или закройте его, прежде чем открывать другой товар.");
            ProductEditWindow.OpenedWindow.Activate();
            return;
        }

        ProductEditWindow window;
        try
        {
            window = new ProductEditWindow(productId) { Owner = this };
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError($"Не удалось открыть карточку товара.\n\nПодробности: {ex.Message}");
            LoadProducts();
            return;
        }

        window.Closed += (_, _) => LoadProducts();
        window.Show();
    }

    private void OrdersButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductEditWindow.OpenedWindow != null)
        {
            MessageHelper.ShowWarning("Сначала сохраните или закройте окно редактирования товара.");
            ProductEditWindow.OpenedWindow.Activate();
            return;
        }

        new OrdersWindow().Show();
        Close();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        AppSession.CurrentUser = null;
        new LoginWindow().Show();
        Close();
    }
}
