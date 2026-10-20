using BookStore.Data.Models;

namespace BookStore.Data.Services;

/// <summary>Разбор состава заказа из строки вида «A112T4, 2, G843H5, 2».</summary>
public static class OrderLineParser
{
    public static bool TryParse(string? text, IReadOnlyDictionary<string, int> productIdsByArticle,
        out List<OrderLine> lines, out string error)
    {
        lines = new List<OrderLine>();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Укажите состав заказа в формате «Артикул, количество, Артикул, количество».";
            return false;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length % 2 != 0)
        {
            error = "После каждого артикула через запятую должно идти количество. Пример: A112T4, 2, G843H5, 1.";
            return false;
        }

        for (var i = 0; i < parts.Length; i += 2)
        {
            // В исходных данных встречались кириллические «А» и «Т» в артикулах — приводим к латинице.
            var article = parts[i].ToUpperInvariant().Replace('А', 'A').Replace('Т', 'T');
            if (!productIdsByArticle.TryGetValue(article, out var productId))
            {
                error = $"Товар с артикулом «{parts[i]}» не найден. Проверьте артикул в списке товаров.";
                return false;
            }

            if (!int.TryParse(parts[i + 1], out var quantity) || quantity <= 0)
            {
                error = $"Количество для артикула «{article}» должно быть целым числом больше нуля.";
                return false;
            }

            if (lines.Any(l => l.ProductId == productId))
            {
                error = $"Артикул «{article}» указан несколько раз. Объедините количество в одну позицию.";
                return false;
            }

            lines.Add(new OrderLine { ProductId = productId, Article = article, Quantity = quantity });
        }
        return true;
    }
}
