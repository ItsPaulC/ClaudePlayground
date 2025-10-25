using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ClaudePlayground.Domain.Entities;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("tenantId")]
    [BsonRepresentation(BsonType.String)]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; set; }
}
