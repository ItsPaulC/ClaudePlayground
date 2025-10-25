using ClaudePlayground.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace ClaudePlayground.Domain.Entities;

public class Business : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("address")]
    public Address? Address { get; set; }

    [BsonElement("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("website")]
    public string? Website { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
