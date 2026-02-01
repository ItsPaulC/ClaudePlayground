using System.Text.RegularExpressions;
using ClaudePlayground.Application.DTOs;
using FluentValidation;

namespace ClaudePlayground.Application.Validators;

public partial class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    [GeneratedRegex(@"[^a-zA-Z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericRegex();

    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(16)
            .WithMessage("Password must be at least 16 characters long (4 words of 4+ characters each)")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters")
            .Must(BeValidPassphrase)
            .WithMessage("Password must contain at least 4 words of 4 or more characters each, separated by non-alphanumeric characters (e.g., 'blue-coffee-mountain-River' or 'Blue@Coffee!Mountain#River')");

        RuleFor(x => x.RoleValues)
            .NotEmpty()
            .WithMessage("At least one role must be specified");
    }

    private static bool BeValidPassphrase(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // Split by non-alphanumeric characters and filter out empty entries
        List<string> words = NonAlphanumericRegex().Split(password)
            .Where(w => !string.IsNullOrEmpty(w))
            .ToList();

        // Count words that are at least 4 characters
        int validWordCount = words.Count(w => w.Length >= 4);

        // Must have at least 4 valid words
        return validWordCount >= 4;
    }
}
