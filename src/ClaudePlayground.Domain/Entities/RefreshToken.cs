using MongoDB.Bson.Serialization.Attributes;

namespace ClaudePlayground.Domain.Entities;

public class RefreshToken : BaseEntity
{
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("isRevoked")]
    public bool IsRevoked { get; set; } = false;

    [BsonElement("revokedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? RevokedAt { get; set; }
}
