using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BookStore.App.Helpers;

public static class ImageHelper
{
    public const int MaxWidth = 300;
    public const int MaxHeight = 200;
    private const string ImagesFolder = "Images";

    public static string ImagesDirectory => Path.Combine(AppContext.BaseDirectory, ImagesFolder);

    private static readonly Lazy<BitmapImage> Placeholder = new(() =>
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri("pack://application:,,,/Resources/picture.png");
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    });

    public static BitmapImage PlaceholderImage => Placeholder.Value;

    public static string? ToFullPath(string? relativePath) =>
        string.IsNullOrWhiteSpace(relativePath) ? null : Path.Combine(AppContext.BaseDirectory, relativePath);

    /// <summary>Загружает фото товара; если пути нет или файл отсутствует — возвращает заглушку picture.png.</summary>
    public static ImageSource LoadProductImage(string? relativePath)
    {
        var fullPath = ToFullPath(relativePath);
        return fullPath != null && File.Exists(fullPath)
            ? LoadFromFile(fullPath) ?? PlaceholderImage
            : PlaceholderImage;
    }

    /// <summary>
    /// Загрузка с CacheOption.OnLoad: файл читается целиком и сразу закрывается,
    /// иначе WPF держит его открытым и старое фото нельзя удалить при замене.
    /// </summary>
    public static BitmapImage? LoadFromFile(string fullPath)
    {
        try
        {
            using var stream = File.OpenRead(fullPath);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Уменьшает изображение так, чтобы оно помещалось в 300×200 (с сохранением пропорций),
    /// сохраняет его в папку Images и возвращает относительный путь для записи в БД.
    /// </summary>
    public static string SaveResized(string sourceFile)
    {
        var source = LoadFromFile(sourceFile)
                     ?? throw new InvalidOperationException("Выбранный файл не является изображением или повреждён.");

        var scale = Math.Min(1.0, Math.Min((double)MaxWidth / source.PixelWidth, (double)MaxHeight / source.PixelHeight));
        BitmapSource result = scale < 1.0
            ? new TransformedBitmap(source, new ScaleTransform(scale, scale))
            : source;

        Directory.CreateDirectory(ImagesDirectory);
        var fileName = $"{Guid.NewGuid():N}.jpg";
        var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
        encoder.Frames.Add(BitmapFrame.Create(result));
        using (var output = File.Create(Path.Combine(ImagesDirectory, fileName)))
        {
            encoder.Save(output);
        }

        return $"{ImagesFolder}/{fileName}";
    }

    public static void DeleteImage(string? relativePath)
    {
        var fullPath = ToFullPath(relativePath);
        try
        {
            if (fullPath != null && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException)
        {
            // Файл занят другим процессом — он останется в папке, на работу программы это не влияет.
        }
    }
}
