using BookStore.Data.Models;
using Microsoft.Data.Sqlite;

namespace BookStore.Data.Repositories;

public class ProductRepository
{
    private const string SelectSql =
        @"SELECT p.ProductId, p.Article, p.ProductName, p.UnitId, un.UnitName, p.Price,
                 p.SupplierId, s.SupplierName, p.ManufacturerId, m.ManufacturerName,
                 p.CategoryId, c.CategoryName, p.Discount, p.StockQuantity, p.Description, p.PhotoPath
          FROM Product p
          JOIN Unit un ON un.UnitId = p.UnitId
          JOIN Supplier s ON s.SupplierId = p.SupplierId
          JOIN Manufacturer m ON m.ManufacturerId = p.ManufacturerId
          JOIN Category c ON c.CategoryId = p.CategoryId";

    public List<Product> GetAll()
    {
        var products = new List<Product>();
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection, SelectSql + " ORDER BY p.ProductId;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            products.Add(ReadProduct(reader));
        }
        return products;
    }

    public Product? GetById(int productId)
    {
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection, SelectSql + " WHERE p.ProductId = @id;");
        command.Parameters.AddWithValue("@id", productId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadProduct(reader) : null;
    }

    /// <summary>Следующий ID товара: максимальный имеющийся + 1.</summary>
    public int GetNextId()
    {
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection,
            "SELECT COALESCE(MAX(ProductId), 0) + 1 FROM Product;");
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public bool ArticleExists(string article, int exceptProductId)
    {
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection,
            "SELECT COUNT(*) FROM Product WHERE Article = @article AND ProductId <> @id;");
        command.Parameters.AddWithValue("@article", article.Trim());
        command.Parameters.AddWithValue("@id", exceptProductId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool IsUsedInOrders(int productId)
    {
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection,
            "SELECT COUNT(*) FROM OrderItem WHERE ProductId = @id;");
        command.Parameters.AddWithValue("@id", productId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>Добавляет товар и возвращает присвоенный ему ID.</summary>
    public int Insert(Product product)
    {
        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var idCommand = Database.CreateCommand(connection,
                   "SELECT COALESCE(MAX(ProductId), 0) + 1 FROM Product;", transaction))
        {
            product.ProductId = Convert.ToInt32(idCommand.ExecuteScalar());
        }

        ResolveLookups(connection, transaction, product);

        using var command = Database.CreateCommand(connection,
            @"INSERT INTO Product (ProductId, Article, ProductName, UnitId, Price, SupplierId, ManufacturerId,
                                   CategoryId, Discount, StockQuantity, Description, PhotoPath)
              VALUES (@id, @article, @name, @unit, @price, @supplier, @manufacturer,
                      @category, @discount, @quantity, @description, @photo);", transaction);
        AddParameters(command, product);
        command.ExecuteNonQuery();

        transaction.Commit();
        return product.ProductId;
    }

    public void Update(Product product)
    {
        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        ResolveLookups(connection, transaction, product);

        using var command = Database.CreateCommand(connection,
            @"UPDATE Product
              SET Article = @article, ProductName = @name, UnitId = @unit, Price = @price,
                  SupplierId = @supplier, ManufacturerId = @manufacturer, CategoryId = @category,
                  Discount = @discount, StockQuantity = @quantity, Description = @description,
                  PhotoPath = @photo
              WHERE ProductId = @id;", transaction);
        AddParameters(command, product);
        command.ExecuteNonQuery();

        transaction.Commit();
    }

    public void Delete(int productId)
    {
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection, "DELETE FROM Product WHERE ProductId = @id;");
        command.Parameters.AddWithValue("@id", productId);
        command.ExecuteNonQuery();
    }

    // Производитель и единица измерения вводятся на форме текстом, поэтому перед сохранением
    // находим (или добавляем) соответствующую запись справочника.
    private static void ResolveLookups(SqliteConnection connection, SqliteTransaction transaction, Product product)
    {
        product.ManufacturerId = LookupRepository.GetOrCreateId(connection, transaction,
            "Manufacturer", "ManufacturerId", "ManufacturerName", product.ManufacturerName);
        product.UnitId = LookupRepository.GetOrCreateId(connection, transaction,
            "Unit", "UnitId", "UnitName", product.UnitName);
    }

    private static void AddParameters(SqliteCommand command, Product product)
    {
        command.Parameters.AddWithValue("@id", product.ProductId);
        command.Parameters.AddWithValue("@article", product.Article.Trim());
        command.Parameters.AddWithValue("@name", product.ProductName.Trim());
        command.Parameters.AddWithValue("@unit", product.UnitId);
        command.Parameters.AddWithValue("@price", (double)product.Price);
        command.Parameters.AddWithValue("@supplier", product.SupplierId);
        command.Parameters.AddWithValue("@manufacturer", product.ManufacturerId);
        command.Parameters.AddWithValue("@category", product.CategoryId);
        command.Parameters.AddWithValue("@discount", product.Discount);
        command.Parameters.AddWithValue("@quantity", product.StockQuantity);
        command.Parameters.AddWithValue("@description", Database.ToDbValue(product.Description));
        command.Parameters.AddWithValue("@photo", Database.ToDbValue(product.PhotoPath));
    }

    private static Product ReadProduct(SqliteDataReader reader)
    {
        return new Product
        {
            ProductId = reader.GetInt32(0),
            Article = reader.GetString(1),
            ProductName = reader.GetString(2),
            UnitId = reader.GetInt32(3),
            UnitName = reader.GetString(4),
            Price = Math.Round((decimal)reader.GetDouble(5), 2),
            SupplierId = reader.GetInt32(6),
            SupplierName = reader.GetString(7),
            ManufacturerId = reader.GetInt32(8),
            ManufacturerName = reader.GetString(9),
            CategoryId = reader.GetInt32(10),
            CategoryName = reader.GetString(11),
            Discount = reader.GetInt32(12),
            StockQuantity = reader.GetInt32(13),
            Description = reader.IsDBNull(14) ? null : reader.GetString(14),
            PhotoPath = reader.IsDBNull(15) ? null : reader.GetString(15)
        };
    }
}
