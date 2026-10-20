using Microsoft.Data.Sqlite;

namespace BookStore.Data;

/// <summary>Подключение к файлу базы SQLite. Если файла нет, БД создаётся из скрипта BookStore.sql.</summary>
public static class Database
{
    public const string DateFormat = "yyyy-MM-dd";

    public static string DatabasePath { get; private set; } =
        Path.Combine(AppContext.BaseDirectory, "BookStore.db");

    public static string ScriptPath { get; private set; } =
        Path.Combine(AppContext.BaseDirectory, "BookStore.sql");

    public static void Configure(string databasePath, string scriptPath)
    {
        DatabasePath = databasePath;
        ScriptPath = scriptPath;
    }

    public static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();

        // В SQLite проверка внешних ключей включается отдельно для каждого подключения.
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    public static void EnsureCreated()
    {
        if (File.Exists(DatabasePath))
        {
            return;
        }

        if (!File.Exists(ScriptPath))
        {
            throw new FileNotFoundException(
                "Не найден файл базы данных и скрипт для её создания.", ScriptPath);
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = File.ReadAllText(ScriptPath);
        command.ExecuteNonQuery();
    }

    public static SqliteCommand CreateCommand(SqliteConnection connection, string sql,
        SqliteTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }

    public static object ToDbValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
