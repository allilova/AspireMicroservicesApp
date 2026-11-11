using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shop.NotificationsApi.Data;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Type { get; set; } = "Info";
    public string Message { get; set; } = "";
    public string? OrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}