using ClaudePlayground.Domain.ValueObjects;

namespace ClaudePlayground.Domain.Common;

public static class Roles
{
    // Role value constants (for JWT claims and authorization)
    public const string SuperUserValue = "super-user";
    public const string AdminValue = "admin";

    // Role objects
    public static readonly Role SuperUser = new("Super User", "SU", SuperUserValue);
    public static readonly Role Admin = new("Administrator", "ADM", AdminValue);

    // Helper method to get role by value
    public static Role? GetByValue(string value)
    {
        return value switch
        {
            SuperUserValue => SuperUser,
            AdminValue => Admin,
            _ => null
        };
    }

    // Helper method to get all roles
    public static IEnumerable<Role> GetAll()
    {
        return new[] { SuperUser, Admin };
    }
}
