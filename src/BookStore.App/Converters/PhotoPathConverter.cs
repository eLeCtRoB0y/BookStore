using System.Globalization;
using System.Windows.Data;
using BookStore.App.Helpers;

namespace BookStore.App.Converters;

/// <summary>Преобразует путь к фото из БД в изображение (или заглушку, если фото нет).</summary>
public class PhotoPathConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ImageHelper.LoadProductImage(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
