namespace ClaudePlayground.Application.DTOs;

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    string TenantId,
    string Email,
    string? FirstName,
    string? LastName
);
