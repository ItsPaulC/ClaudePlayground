using ClaudePlayground.Domain.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ClaudePlayground.Infrastructure.Persistence.Mapping;

public static class UserMap
{
    public static void Configure()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(User)))
            return;

        BsonClassMap.RegisterClassMap<User>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapMember(c => c.Email).SetElementName("email");
            cm.MapMember(c => c.PasswordHash).SetElementName("passwordHash");
            cm.MapMember(c => c.FirstName).SetElementName("firstName");
            cm.MapMember(c => c.LastName).SetElementName("lastName");
            cm.MapMember(c => c.IsActive).SetElementName("isActive");
            cm.MapMember(c => c.Roles).SetElementName("roles");
            cm.MapMember(c => c.IsEmailVerified).SetElementName("isEmailVerified");
            cm.MapMember(c => c.EmailVerificationToken).SetElementName("emailVerificationToken");
            cm.MapMember(c => c.EmailVerificationTokenExpiresAt)
                .SetElementName("emailVerificationTokenExpiresAt")
                .SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Utc)));
            cm.MapMember(c => c.PasswordResetToken).SetElementName("passwordResetToken");
            cm.MapMember(c => c.PasswordResetTokenExpiresAt)
                .SetElementName("passwordResetTokenExpiresAt")
                .SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Utc)));
            cm.MapMember(c => c.LastLoginAt)
                .SetElementName("lastLoginAt")
                .SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Utc)));
        });
    }
}
