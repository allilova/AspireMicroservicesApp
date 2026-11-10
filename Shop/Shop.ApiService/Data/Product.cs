using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shop.CatalogApi.Data
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string? Description { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }
    }
}