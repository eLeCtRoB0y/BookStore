namespace BookStore.Data.Models;

public class Product
{
    /// <summary>Порог скидки, после которого строка подсвечивается цветом #23E1EF.</summary>
    public const int BigDiscountThreshold = 25;

    public int ProductId { get; set; }
    public string Article { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Discount { get; set; }
    public int StockQuantity { get; set; }
    public string? Description { get; set; }
    public string? PhotoPath { get; set; }

    public bool HasDiscount => Discount > 0;
    public bool IsBigDiscount => Discount > BigDiscountThreshold;
    public bool IsOutOfStock => StockQuantity <= 0;
    public decimal FinalPrice => Math.Round(Price * (100 - Discount) / 100m, 2);

    public string Header => $"{CategoryName} | {ProductName}";
    public string PriceText => $"{Price:N2} ₽";
    public string FinalPriceText => $"{FinalPrice:N2} ₽";
    public string DiscountText => $"{Discount}%";
}
