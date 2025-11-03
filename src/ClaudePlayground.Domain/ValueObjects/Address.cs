namespace ClaudePlayground.Domain.ValueObjects;

public class Address
{
    public string? Street { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

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
