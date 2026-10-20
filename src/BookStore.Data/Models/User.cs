namespace BookStore.Data.Models;

/// <summary>Роли пользователей (значения совпадают с Role.RoleId в БД).</summary>
public enum UserRole
{
    Guest = 0,
    Administrator = 1,
    Manager = 2,
    Client = 3
}

public class User
{
    public int UserId { get; set; }
    public UserRole Role { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;

    public bool CanManageCatalog => Role == UserRole.Manager || Role == UserRole.Administrator;
    public bool IsAdministrator => Role == UserRole.Administrator;
}
