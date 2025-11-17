using ClaudePlayground.Domain.ValueObjects;

namespace ClaudePlayground.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<Role> Roles { get; set; } = [];

    public bool IsEmailVerified { get; set; } = false;

    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
