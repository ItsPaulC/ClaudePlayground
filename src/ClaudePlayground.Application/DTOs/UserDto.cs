namespace ClaudePlayground.Application.DTOs;

public record UserDto(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);
