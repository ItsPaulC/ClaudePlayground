namespace ClaudePlayground.Domain.ValueObjects;

public sealed class Role
{
    public string Name { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public Role()
    {
    }

    public Role(string name, string abbreviation, string value)
    {
        Name = name;
        Abbreviation = abbreviation;
        Value = value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Role role && Value == role.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Role? left, Role? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Role? left, Role? right)
    {
        return !(left == right);
    }
}
