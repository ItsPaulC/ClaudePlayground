namespace ClaudePlayground.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default);
}
