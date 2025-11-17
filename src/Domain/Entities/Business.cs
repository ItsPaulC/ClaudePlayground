using ClaudePlayground.Domain.ValueObjects;

namespace ClaudePlayground.Domain.Entities;

public class Business : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Address? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;
}
