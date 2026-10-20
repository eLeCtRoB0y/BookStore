using BookStore.Data.Models;
using Microsoft.Data.Sqlite;

namespace BookStore.Data.Repositories;

/// <summary>Чтение справочников и поиск/добавление значений в них.</summary>
public static class LookupRepository
{
    public static List<LookupItem> GetItems(string sql)
    {
        var items = new List<LookupItem>();
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection, sql);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new LookupItem { Id = reader.GetInt32(0), Name = reader.GetString(1) });
        }
        return items;
    }

    public static List<LookupItem> GetCategories() =>
        GetItems("SELECT CategoryId, CategoryName FROM Category ORDER BY CategoryName;");

    public static List<LookupItem> GetSuppliers() =>
        GetItems("SELECT SupplierId, SupplierName FROM Supplier ORDER BY SupplierName;");

    public static List<LookupItem> GetManufacturers() =>
        GetItems("SELECT ManufacturerId, ManufacturerName FROM Manufacturer ORDER BY ManufacturerName;");

    public static List<LookupItem> GetUnits() =>
        GetItems("SELECT UnitId, UnitName FROM Unit ORDER BY UnitName;");

    public static List<LookupItem> GetOrderStatuses() =>
        GetItems("SELECT OrderStatusId, StatusName FROM OrderStatus ORDER BY OrderStatusId;");

    public static List<LookupItem> GetPickupPoints() =>
        GetItems("SELECT PickupPointId, Address FROM PickupPoint ORDER BY Address;");

    /// <summary>
    /// Возвращает Id значения справочника; если такого значения нет, добавляет его.
    /// Используется для полей, которые на форме вводятся текстом (производитель, единица измерения).
    /// Имена таблицы и столбцов задаются только из кода, не пользователем.
    /// </summary>
    public static int GetOrCreateId(SqliteConnection connection, SqliteTransaction transaction,
        string table, string idColumn, string nameColumn, string value)
    {
        var name = value.Trim();
        using (var find = Database.CreateCommand(connection,
                   $"SELECT {idColumn} FROM {table} WHERE {nameColumn} = @name;", transaction))
        {
            find.Parameters.AddWithValue("@name", name);
            var existing = find.ExecuteScalar();
            if (existing != null && existing != DBNull.Value)
            {
                return Convert.ToInt32(existing);
            }
        }

        using var insert = Database.CreateCommand(connection,
            $"INSERT INTO {table} ({nameColumn}) VALUES (@name); SELECT last_insert_rowid();", transaction);
        insert.Parameters.AddWithValue("@name", name);
        return Convert.ToInt32(insert.ExecuteScalar());
    }
}
