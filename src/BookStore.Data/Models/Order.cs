namespace BookStore.Data.Models;

public class OrderLine
{
    public int ProductId { get; set; }
    public string Article { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public int PickupPointId { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int ReceiveCode { get; set; }
    public int OrderStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public List<OrderLine> Lines { get; set; } = new();

    /// <summary>Состав заказа в исходном формате: «Артикул, кол-во, Артикул, кол-во».</summary>
    public string ArticleText => string.Join(", ", Lines.Select(l => $"{l.Article}, {l.Quantity}"));

    public string OrderDateText => OrderDate.ToString("dd.MM.yyyy");
    public string DeliveryDateText => DeliveryDate.ToString("dd.MM.yyyy");
}
