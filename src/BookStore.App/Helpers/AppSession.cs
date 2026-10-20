using BookStore.Data.Models;

namespace BookStore.App.Helpers;

/// <summary>Текущий пользователь. null — вход выполнен в роли гостя.</summary>
public static class AppSession
{
    public static User? CurrentUser { get; set; }

    public static UserRole Role => CurrentUser?.Role ?? UserRole.Guest;

    public static string DisplayName => CurrentUser?.FullName ?? "Гость";

    public static bool CanManageCatalog => CurrentUser?.CanManageCatalog ?? false;

    public static bool IsAdministrator => CurrentUser?.IsAdministrator ?? false;
}
