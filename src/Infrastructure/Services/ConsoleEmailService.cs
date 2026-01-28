using ClaudePlayground.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClaudePlayground.Infrastructure.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        // In production, this would send an actual email
        // For now, log the verification link to console for testing
        string verificationLink = $"https://localhost:5001/api/auth/verify-email?token={verificationToken}";

        _logger.LogInformation(
            "=== EMAIL VERIFICATION ===\n" +
            "To: {Email}\n" +
            "Subject: Verify your email address\n" +
            "Verification Link: {VerificationLink}\n" +
            "Token: {Token}\n" +
            "========================",
            email, verificationLink, verificationToken);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        // In production, this would send an actual email
        // For now, log the password reset link to console for testing
        string resetLink = $"https://localhost:5001/api/auth/reset-password?token={resetToken}";

        _logger.LogInformation(
            "=== PASSWORD RESET ===\n" +
            "To: {Email}\n" +
            "Subject: Reset your password\n" +
            "Reset Link: {ResetLink}\n" +
            "Token: {Token}\n" +
            "This link expires in 1 hour.\n" +
            "===================",
            email, resetLink, resetToken);

        return Task.CompletedTask;
    }
}
