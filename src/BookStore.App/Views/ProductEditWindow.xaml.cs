using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BookStore.App.Helpers;
using BookStore.Data.Models;
using BookStore.Data.Repositories;
using Microsoft.Win32;

namespace BookStore.App.Views;

public partial class ProductEditWindow : Window
{
    /// <summary>Открытое окно редактирования (одновременно допускается только одно).</summary>
    public static ProductEditWindow? OpenedWindow { get; private set; }

    private readonly ProductRepository _productRepository = new();
    private readonly Product? _product;
    private string? _newPhotoSourceFile;

    public ProductEditWindow(int? productId)
    {
        InitializeComponent();

        LoadLookups();

        if (productId.HasValue)
        {
            _product = _productRepository.GetById(productId.Value)
                       ?? throw new InvalidOperationException("Товар не найден в базе данных: возможно, он уже удалён.");
        }

        if (_product == null)
        {
            Title = "Добавление товара";
            HeaderText.Text = "Добавление товара";
            // ID нового товара не показывается: он вычисляется при сохранении (MAX + 1).
            IdLabel.Visibility = Visibility.Collapsed;
            IdBox.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
            UnitBox.Text = "шт.";
            QuantityBox.Text = "0";
            DiscountBox.Text = "0";
            PhotoImage.Source = ImageHelper.PlaceholderImage;
        }
        else
        {
            Title = "Редактирование товара";
            HeaderText.Text = $"Редактирование товара «{_product.ProductName}»";
            FillFields(_product);
        }

        // Регистрируем окно только после успешной загрузки данных.
        OpenedWindow = this;
    }

    private void LoadLookups()
    {
        CategoryBox.ItemsSource = LookupRepository.GetCategories();
        SupplierBox.ItemsSource = LookupRepository.GetSuppliers();
        // Для редактируемых списков используются строки: в поле можно ввести и новое значение.
        ManufacturerBox.ItemsSource = LookupRepository.GetManufacturers().Select(m => m.Name).ToList();
        UnitBox.ItemsSource = LookupRepository.GetUnits().Select(u => u.Name).ToList();
    }

    private void FillFields(Product product)
    {
        IdBox.Text = product.ProductId.ToString();
        ArticleBox.Text = product.Article;
        NameBox.Text = product.ProductName;
        CategoryBox.SelectedValue = product.CategoryId;
        DescriptionBox.Text = product.Description ?? string.Empty;
        ManufacturerBox.Text = product.ManufacturerName;
        SupplierBox.SelectedValue = product.SupplierId;
        PriceBox.Text = product.Price.ToString("0.00", CultureInfo.CurrentCulture);
        UnitBox.Text = product.UnitName;
        QuantityBox.Text = product.StockQuantity.ToString();
        DiscountBox.Text = product.Discount.ToString();
        PhotoImage.Source = ImageHelper.LoadProductImage(product.PhotoPath);
    }

    private void ChoosePhotoButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выбор изображения товара",
            Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var preview = ImageHelper.LoadFromFile(dialog.FileName);
        if (preview == null)
        {
            MessageHelper.ShowError("Не удалось открыть выбранный файл как изображение.\n\nВыберите файл в формате JPG, PNG или BMP.");
            return;
        }

        // Сам файл копируется в папку программы только при сохранении, чтобы при отмене не оставалось мусора.
        _newPhotoSourceFile = dialog.FileName;
        PhotoImage.Source = preview;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var errors = new List<string>();
        ResetHighlight();

        var article = ArticleBox.Text.Trim();
        var name = NameBox.Text.Trim();
        var manufacturer = ManufacturerBox.Text.Trim();
        var unit = UnitBox.Text.Trim();

        Require(article.Length > 0, ArticleBox, "Укажите артикул товара.", errors);
        Require(name.Length > 0, NameBox, "Укажите наименование товара.", errors);
        Require(CategoryBox.SelectedValue != null, CategoryBox, "Выберите категорию товара из списка.", errors);
        Require(manufacturer.Length > 0, ManufacturerBox, "Укажите производителя (выберите из списка или введите).", errors);
        Require(SupplierBox.SelectedValue != null, SupplierBox, "Выберите поставщика из списка.", errors);
        Require(unit.Length > 0, UnitBox, "Укажите единицу измерения, например «шт.».", errors);

        var priceOk = TryParsePrice(PriceBox.Text, out var price);
        Require(priceOk, PriceBox,
            "Цена должна быть неотрицательным числом, не более двух знаков после запятой (например, 585,50).", errors);

        var quantityOk = int.TryParse(QuantityBox.Text.Trim(), out var quantity) && quantity >= 0;
        Require(quantityOk, QuantityBox, "Количество на складе должно быть целым неотрицательным числом.", errors);

        var discountOk = int.TryParse(DiscountBox.Text.Trim(), out var discount) && discount is >= 0 and <= 100;
        Require(discountOk, DiscountBox, "Скидка должна быть целым числом от 0 до 100.", errors);

        try
        {
            if (article.Length > 0 && _productRepository.ArticleExists(article, _product?.ProductId ?? 0))
            {
                Require(false, ArticleBox, $"Товар с артикулом «{article}» уже существует. Укажите другой артикул.", errors);
            }
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError($"Не удалось проверить артикул в базе данных.\n\nПодробности: {ex.Message}");
            return;
        }

        if (errors.Count > 0)
        {
            MessageHelper.ShowWarning("Товар не сохранён. Исправьте ошибки (поля выделены красным):\n\n• " +
                                      string.Join("\n• ", errors));
            return;
        }

        var oldPhotoPath = _product?.PhotoPath;
        var product = _product ?? new Product();
        product.Article = article;
        product.ProductName = name;
        product.CategoryId = (int)CategoryBox.SelectedValue!;
        product.Description = DescriptionBox.Text;
        product.ManufacturerName = manufacturer;
        product.SupplierId = (int)SupplierBox.SelectedValue!;
        product.Price = price;
        product.UnitName = unit;
        product.StockQuantity = quantity;
        product.Discount = discount;

        string? savedPhotoPath = null;
        try
        {
            if (_newPhotoSourceFile != null)
            {
                savedPhotoPath = ImageHelper.SaveResized(_newPhotoSourceFile);
                product.PhotoPath = savedPhotoPath;
            }

            if (_product == null)
            {
                _productRepository.Insert(product);
            }
            else
            {
                _productRepository.Update(product);
            }
        }
        catch (Exception ex)
        {
            // Если запись в БД не удалась, только что скопированное фото не нужно.
            ImageHelper.DeleteImage(savedPhotoPath);
            product.PhotoPath = oldPhotoPath;
            MessageHelper.ShowError($"Не удалось сохранить товар.\n\nПодробности: {ex.Message}");
            return;
        }

        // При замене изображения старый файл удаляется из папки программы.
        if (savedPhotoPath != null && !string.IsNullOrEmpty(oldPhotoPath))
        {
            ImageHelper.DeleteImage(oldPhotoPath);
        }

        MessageHelper.ShowInfo(_product == null
            ? $"Товар «{product.ProductName}» добавлен (ID {product.ProductId})."
            : $"Изменения товара «{product.ProductName}» сохранены.");
        Close();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_product == null)
        {
            return;
        }

        try
        {
            if (_productRepository.IsUsedInOrders(_product.ProductId))
            {
                MessageHelper.ShowError(
                    $"Товар «{_product.ProductName}» нельзя удалить, так как он присутствует в заказах.\n\n" +
                    "Чтобы убрать товар из продажи, установите количество на складе равным 0.");
                return;
            }

            if (!MessageHelper.Confirm(
                    $"Удалить товар «{_product.ProductName}» (артикул {_product.Article})?\n\nЭто действие нельзя отменить."))
            {
                return;
            }

            _productRepository.Delete(_product.ProductId);
            ImageHelper.DeleteImage(_product.PhotoPath);
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError($"Не удалось удалить товар.\n\nПодробности: {ex.Message}");
            return;
        }

        MessageHelper.ShowInfo("Товар удалён.");
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

    private static bool TryParsePrice(string text, out decimal price)
    {
        // Принимаем и запятую, и точку в качестве разделителя дробной части.
        var normalized = text.Trim().Replace(" ", string.Empty).Replace(',', '.');
        var ok = decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price);
        return ok && price >= 0 && decimal.Round(price, 2) == price;
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
                     ArticleBox, NameBox, CategoryBox, ManufacturerBox, SupplierBox,
                     PriceBox, UnitBox, QuantityBox, DiscountBox
                 })
        {
            control.ClearValue(BorderBrushProperty);
            control.ClearValue(BorderThicknessProperty);
        }
    }
}
