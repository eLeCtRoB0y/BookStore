namespace BookStore.Data.Models;

/// <summary>Элемент справочника (категория, поставщик, статус и т.п.).</summary>
public class LookupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
