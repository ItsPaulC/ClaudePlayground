using ClaudePlayground.Domain.ValueObjects;

namespace ClaudePlayground.Application.DTOs;

public record UserDto(
    string Id,
    string TenantId,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    IEnumerable<Role> Roles,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateUserDto(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    IEnumerable<string> RoleValues
);

public record UpdateUserDto(
    string? FirstName,
    string? LastName,
    bool IsActive,
    IEnumerable<string> RoleValues
);
