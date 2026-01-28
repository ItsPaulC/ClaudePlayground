using ClaudePlayground.Domain.ValueObjects;
using MongoDB.Bson.Serialization;

namespace ClaudePlayground.Infrastructure.Persistence.Mapping;

public static class AddressMap
{
    public static void Configure()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Address)))
            return;

        BsonClassMap.RegisterClassMap<Address>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapMember(c => c.Street).SetElementName("street");
            cm.MapMember(c => c.City).SetElementName("city");
            cm.MapMember(c => c.State).SetElementName("state");
            cm.MapMember(c => c.ZipCode).SetElementName("zipCode");
            cm.MapMember(c => c.Country).SetElementName("country");
        });
    }
}
