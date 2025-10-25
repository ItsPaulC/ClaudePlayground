using MongoDB.Bson.Serialization.Attributes;

namespace ClaudePlayground.Domain.ValueObjects;

public class Address
{
    [BsonElement("street")]
    public string? Street { get; set; }

    [BsonElement("city")]
    public string? City { get; set; }

    [BsonElement("state")]
    public string? State { get; set; }

    [BsonElement("zipCode")]
    public string? ZipCode { get; set; }

    [BsonElement("country")]
    public string? Country { get; set; }

    public Address()
    {
    }

    public Address(string? street, string? city, string? state, string? zipCode, string? country)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(Street) &&
               string.IsNullOrWhiteSpace(City) &&
               string.IsNullOrWhiteSpace(State) &&
               string.IsNullOrWhiteSpace(ZipCode) &&
               string.IsNullOrWhiteSpace(Country);
    }
}
