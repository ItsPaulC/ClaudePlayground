namespace ClaudePlayground.Application.DTOs;

public record ResetPasswordDto(
    string Token,
    string NewPassword
);
