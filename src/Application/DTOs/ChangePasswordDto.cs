namespace ClaudePlayground.Application.DTOs;

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);
