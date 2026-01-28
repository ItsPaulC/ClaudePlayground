using ClaudePlayground.Domain.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ClaudePlayground.Infrastructure.Persistence.Mapping;

public static class RefreshTokenMap
{
    public static void Configure()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(RefreshToken)))
            return;

        BsonClassMap.RegisterClassMap<RefreshToken>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapMember(c => c.Token).SetElementName("token");
            cm.MapMember(c => c.UserId).SetElementName("userId");
            cm.MapMember(c => c.ExpiresAt)
                .SetElementName("expiresAt")
                .SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
            cm.MapMember(c => c.IsRevoked).SetElementName("isRevoked");
            cm.MapMember(c => c.RevokedAt)
                .SetElementName("revokedAt")
                .SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Utc)));
        });
    }
}
