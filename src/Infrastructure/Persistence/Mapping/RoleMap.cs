using ClaudePlayground.Domain.ValueObjects;
using MongoDB.Bson.Serialization;

namespace ClaudePlayground.Infrastructure.Persistence.Mapping;

public static class RoleMap
{
    public static void Configure()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Role)))
            return;

        BsonClassMap.RegisterClassMap<Role>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapMember(c => c.Name).SetElementName("name");
            cm.MapMember(c => c.Abbreviation).SetElementName("abbreviation");
            cm.MapMember(c => c.Value).SetElementName("value");
        });
    }
}
