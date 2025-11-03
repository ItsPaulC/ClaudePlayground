using ClaudePlayground.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace ClaudePlayground.Infrastructure.Persistence.Mapping;

public static class BusinessMap
{
    public static void Configure()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Business)))
            return;

        BsonClassMap.RegisterClassMap<Business>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapMember(c => c.Name).SetElementName("name");
            cm.MapMember(c => c.Description).SetElementName("description");
            cm.MapMember(c => c.Address).SetElementName("address");
            cm.MapMember(c => c.PhoneNumber).SetElementName("phoneNumber");
            cm.MapMember(c => c.Email).SetElementName("email");
            cm.MapMember(c => c.Website).SetElementName("website");
            cm.MapMember(c => c.IsActive).SetElementName("isActive");
        });
    }
}
