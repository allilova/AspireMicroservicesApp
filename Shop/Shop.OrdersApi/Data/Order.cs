namespace Shop.OrdersApi.Data;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);
}