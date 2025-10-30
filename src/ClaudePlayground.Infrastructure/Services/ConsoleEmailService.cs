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
}
