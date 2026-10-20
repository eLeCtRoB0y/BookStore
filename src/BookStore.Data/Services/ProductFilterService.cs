using BookStore.Data.Models;

namespace BookStore.Data.Services;

public enum DiscountRange
{
    All,
    From0To12,
    From13To16,
    From17
}

public enum ProductSort
{
    None,
    PriceAscending,
    PriceDescending,
    QuantityAscending,
    QuantityDescending
}

/// <summary>Совместное применение поиска, фильтра по скидке и сортировки к списку товаров.</summary>
public static class ProductFilterService
{
    public static List<Product> Apply(IEnumerable<Product> products, string? searchText,
        DiscountRange range, ProductSort sort)
    {
        var query = products.Where(p => MatchesSearch(p, searchText) && MatchesRange(p, range));

        query = sort switch
        {
            ProductSort.PriceAscending => query.OrderBy(p => p.FinalPrice),
            ProductSort.PriceDescending => query.OrderByDescending(p => p.FinalPrice),
            ProductSort.QuantityAscending => query.OrderBy(p => p.StockQuantity),
            ProductSort.QuantityDescending => query.OrderByDescending(p => p.StockQuantity),
            _ => query
        };
        return query.ToList();
    }

    public static bool MatchesRange(Product product, DiscountRange range)
    {
        decimal discount = product.Discount;
        return range switch
        {
            DiscountRange.From0To12 => discount >= 0 && discount < 13,
            DiscountRange.From13To16 => discount >= 13 && discount < 17,
            DiscountRange.From17 => discount >= 17,
            _ => true
        };
    }

    /// <summary>
    /// Строка поиска разбивается на слова; каждое слово должно встретиться хотя бы в одном
    /// текстовом поле товара. Так поиск работает по нескольким атрибутам одновременно,
    /// например «Кэрролл Прогресс».
    /// </summary>
    public static bool MatchesSearch(Product product, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var fields = new[]
        {
            product.Article, product.ProductName, product.CategoryName, product.Description,
            product.ManufacturerName, product.SupplierName, product.UnitName
        };

        var words = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.All(word => fields.Any(field =>
            field != null && field.Contains(word, StringComparison.CurrentCultureIgnoreCase)));
    }
}
