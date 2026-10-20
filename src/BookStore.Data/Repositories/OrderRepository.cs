using System.Globalization;
using BookStore.Data.Models;
using Microsoft.Data.Sqlite;

namespace BookStore.Data.Repositories;

public class OrderRepository
{
    private const string SelectSql =
        @"SELECT o.OrderId, o.OrderDate, o.DeliveryDate, o.PickupPointId, pp.Address,
                 o.UserId, u.FullName, o.ReceiveCode, o.OrderStatusId, st.StatusName
          FROM CustomerOrder o
          JOIN PickupPoint pp ON pp.PickupPointId = o.PickupPointId
          JOIN AppUser u ON u.UserId = o.UserId
          JOIN OrderStatus st ON st.OrderStatusId = o.OrderStatusId";

    public List<Order> GetAll()
    {
        var orders = new List<Order>();
        using var connection = Database.OpenConnection();
        using (var command = Database.CreateCommand(connection, SelectSql + " ORDER BY o.OrderId;"))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                orders.Add(ReadOrder(reader));
            }
        }

        var byId = orders.ToDictionary(o => o.OrderId);
        foreach (var line in ReadLines(connection, null))
        {
            if (byId.TryGetValue(line.OrderId, out var order))
            {
                order.Lines.Add(line.Line);
            }
        }
        return orders;
    }

    public Order? GetById(int orderId)
    {
        using var connection = Database.OpenConnection();
        Order? order;
        using (var command = Database.CreateCommand(connection, SelectSql + " WHERE o.OrderId = @id;"))
        {
            command.Parameters.AddWithValue("@id", orderId);
            using var reader = command.ExecuteReader();
            order = reader.Read() ? ReadOrder(reader) : null;
        }

        if (order != null)
        {
            order.Lines.AddRange(ReadLines(connection, orderId).Select(l => l.Line));
        }
        return order;
    }

    public int GetNextId() => Scalar("SELECT COALESCE(MAX(OrderId), 0) + 1 FROM CustomerOrder;");

    public int GetNextReceiveCode() => Scalar("SELECT COALESCE(MAX(ReceiveCode), 900) + 1 FROM CustomerOrder;");

    public int Insert(Order order)
    {
        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var idCommand = Database.CreateCommand(connection,
                   "SELECT COALESCE(MAX(OrderId), 0) + 1 FROM CustomerOrder;", transaction))
        {
            order.OrderId = Convert.ToInt32(idCommand.ExecuteScalar());
        }

        using (var command = Database.CreateCommand(connection,
                   @"INSERT INTO CustomerOrder (OrderId, OrderDate, DeliveryDate, PickupPointId, UserId,
                                               ReceiveCode, OrderStatusId)
                     VALUES (@id, @orderDate, @deliveryDate, @point, @user, @code, @status);", transaction))
        {
            AddParameters(command, order);
            command.ExecuteNonQuery();
        }

        SaveLines(connection, transaction, order);
        transaction.Commit();
        return order.OrderId;
    }

    public void Update(Order order)
    {
        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var command = Database.CreateCommand(connection,
                   @"UPDATE CustomerOrder
                     SET OrderDate = @orderDate, DeliveryDate = @deliveryDate, PickupPointId = @point,
                         UserId = @user, ReceiveCode = @code, OrderStatusId = @status
                     WHERE OrderId = @id;", transaction))
        {
            AddParameters(command, order);
            command.ExecuteNonQuery();
        }

        SaveLines(connection, transaction, order);
        transaction.Commit();
    }

    /// <summary>Удаляет заказ; его позиции удаляются каскадно (ON DELETE CASCADE).</summary>
    public void Delete(int orderId)
    {
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection, "DELETE FROM CustomerOrder WHERE OrderId = @id;");
        command.Parameters.AddWithValue("@id", orderId);
        command.ExecuteNonQuery();
    }

    /// <summary>Словарь «артикул → ID товара» для разбора состава заказа.</summary>
    public Dictionary<string, int> GetProductIdsByArticle()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in LookupRepository.GetItems("SELECT ProductId, Article FROM Product;"))
        {
            result[item.Name] = item.Id;
        }
        return result;
    }

    // Состав заказа перезаписывается целиком: так проще, чем вычислять разницу строк.
    private static void SaveLines(SqliteConnection connection, SqliteTransaction transaction, Order order)
    {
        using (var delete = Database.CreateCommand(connection,
                   "DELETE FROM OrderItem WHERE OrderId = @id;", transaction))
        {
            delete.Parameters.AddWithValue("@id", order.OrderId);
            delete.ExecuteNonQuery();
        }

        foreach (var line in order.Lines)
        {
            using var insert = Database.CreateCommand(connection,
                "INSERT INTO OrderItem (OrderId, ProductId, Quantity) VALUES (@order, @product, @quantity);",
                transaction);
            insert.Parameters.AddWithValue("@order", order.OrderId);
            insert.Parameters.AddWithValue("@product", line.ProductId);
            insert.Parameters.AddWithValue("@quantity", line.Quantity);
            insert.ExecuteNonQuery();
        }
    }

    private static List<(int OrderId, OrderLine Line)> ReadLines(SqliteConnection connection, int? orderId)
    {
        var sql = @"SELECT oi.OrderId, oi.ProductId, p.Article, oi.Quantity
                    FROM OrderItem oi
                    JOIN Product p ON p.ProductId = oi.ProductId"
                  + (orderId.HasValue ? " WHERE oi.OrderId = @id" : string.Empty)
                  + " ORDER BY oi.OrderId, oi.rowid;";
        var lines = new List<(int, OrderLine)>();
        using var command = Database.CreateCommand(connection, sql);
        if (orderId.HasValue)
        {
            command.Parameters.AddWithValue("@id", orderId.Value);
        }
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lines.Add((reader.GetInt32(0), new OrderLine
            {
                ProductId = reader.GetInt32(1),
                Article = reader.GetString(2),
                Quantity = reader.GetInt32(3)
            }));
        }
        return lines;
    }

    private static void AddParameters(SqliteCommand command, Order order)
    {
        command.Parameters.AddWithValue("@id", order.OrderId);
        command.Parameters.AddWithValue("@orderDate", order.OrderDate.ToString(Database.DateFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@deliveryDate", order.DeliveryDate.ToString(Database.DateFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@point", order.PickupPointId);
        command.Parameters.AddWithValue("@user", order.UserId);
        command.Parameters.AddWithValue("@code", order.ReceiveCode);
        command.Parameters.AddWithValue("@status", order.OrderStatusId);
    }

    private static Order ReadOrder(SqliteDataReader reader)
    {
        return new Order
        {
            OrderId = reader.GetInt32(0),
            OrderDate = DateTime.ParseExact(reader.GetString(1), Database.DateFormat, CultureInfo.InvariantCulture),
            DeliveryDate = DateTime.ParseExact(reader.GetString(2), Database.DateFormat, CultureInfo.InvariantCulture),
            PickupPointId = reader.GetInt32(3),
            PickupAddress = reader.GetString(4),
            UserId = reader.GetInt32(5),
            ClientName = reader.GetString(6),
            ReceiveCode = reader.GetInt32(7),
            OrderStatusId = reader.GetInt32(8),
            StatusName = reader.GetString(9)
        };
    }

    private static int Scalar(string sql)
    {
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection, sql);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
