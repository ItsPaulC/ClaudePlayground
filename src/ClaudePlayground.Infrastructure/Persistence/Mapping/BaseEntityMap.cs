using ClaudePlayground.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ClaudePlayground.Infrastructure.Persistence.Mapping;

public static class BaseEntityMap
{
    public static void Configure<T>() where T : BaseEntity
    {
        BsonClassMap.RegisterClassMap<T>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            // Map Id property
            cm.MapIdMember(c => c.Id)
                .SetSerializer(new StringSerializer(BsonType.String));

            // Map TenantId property
            cm.MapMember(c => c.TenantId)
                .SetElementName("tenantId")
                .SetSerializer(new StringSerializer(BsonType.String));

            // Map CreatedAt property
            cm.MapMember(c => c.CreatedAt)
                .SetElementName("createdAt")
                .SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));

            // Map UpdatedAt property
            cm.MapMember(c => c.UpdatedAt)
                .SetElementName("updatedAt")
                .SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Utc)));
        });
    }
}
