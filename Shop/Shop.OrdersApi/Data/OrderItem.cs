using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shop.OrdersApi.Data;

public class OrderItem
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
}