using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shop.OrdersApi.Data;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string CustomerName { get; set; } = "";
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; set; } = [];

    [BsonIgnore]
    public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);
}