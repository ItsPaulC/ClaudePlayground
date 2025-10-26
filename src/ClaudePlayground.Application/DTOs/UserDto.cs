using ClaudePlayground.Domain.ValueObjects;

namespace ClaudePlayground.Application.DTOs;

public record UserDto(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    IEnumerable<Role> Roles,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);
