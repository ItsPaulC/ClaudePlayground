using ClaudePlayground.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace ClaudePlayground.Domain.Entities;

public class User : BaseEntity
{
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("firstName")]
    public string? FirstName { get; set; }

    [BsonElement("lastName")]
    public string? LastName { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("roles")]
    public IEnumerable<Role> Roles { get; set; } = [];

    [BsonElement("isEmailVerified")]
    public bool IsEmailVerified { get; set; } = false;

    [BsonElement("emailVerificationToken")]
    public string? EmailVerificationToken { get; set; }

    [BsonElement("emailVerificationTokenExpiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    [BsonElement("passwordResetToken")]
    public string? PasswordResetToken { get; set; }

    [BsonElement("passwordResetTokenExpiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    [BsonElement("lastLoginAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastLoginAt { get; set; }
}
