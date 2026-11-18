using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shop.Web.Date;

public class AppUser
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Email { get; set; } = "";          // винаги малки букви
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer";   // "Admin" или "Customer"
}