using BookStore.Data.Models;

namespace BookStore.Data.Repositories;

public class UserRepository
{
    /// <summary>Возвращает пользователя по логину и паролю или null, если пара не найдена.</summary>
    public User? Authenticate(string login, string password)
    {
        using var connection = Database.OpenConnection();
        using var command = Database.CreateCommand(connection,
            @"SELECT u.UserId, u.RoleId, r.RoleName, u.FullName, u.Login
              FROM AppUser u
              JOIN Role r ON r.RoleId = u.RoleId
              WHERE u.Login = @login AND u.Password = @password;");
        command.Parameters.AddWithValue("@login", login.Trim());
        command.Parameters.AddWithValue("@password", password);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new User
        {
            UserId = reader.GetInt32(0),
            Role = (UserRole)reader.GetInt32(1),
            RoleName = reader.GetString(2),
            FullName = reader.GetString(3),
            Login = reader.GetString(4)
        };
    }

    public List<LookupItem> GetClients()
    {
        return LookupRepository.GetItems(
            "SELECT UserId, FullName FROM AppUser WHERE RoleId = 3 ORDER BY FullName;");
    }
}
