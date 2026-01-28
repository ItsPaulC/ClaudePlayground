using ClaudePlayground.Domain.ValueObjects;

namespace ClaudePlayground.Domain.Common;

public static class Roles
{
    // Role value constants (for JWT claims and authorization)
    public const string SuperUserValue = "super-user";
    public const string BusinessOwnerValue = "business-owner";
    public const string UserValue = "user";
    public const string ReadOnlyUserValue = "read-only-user";

    // Role objects
    public static readonly Role SuperUser = new("Super User", "SU", SuperUserValue);
    public static readonly Role BusinessOwner = new("Business Owner", "BO", BusinessOwnerValue);
    public static readonly Role User = new("User", "USR", UserValue);
    public static readonly Role ReadOnlyUser = new("Read Only User", "RO", ReadOnlyUserValue);

    // Helper method to get role by value
    public static Role? GetByValue(string value)
    {
        return value switch
        {
            SuperUserValue => SuperUser,
            BusinessOwnerValue => BusinessOwner,
            UserValue => User,
            ReadOnlyUserValue => ReadOnlyUser,
            _ => null
        };
    }

    // Helper method to get all roles
    public static IEnumerable<Role> GetAll()
    {
        return new[] { SuperUser, BusinessOwner, User, ReadOnlyUser };
    }
}
