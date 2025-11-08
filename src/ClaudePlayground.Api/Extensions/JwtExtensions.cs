// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using ClaudePlayground.Application.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ClaudePlayground.Api.Extensions;

public static class JwtExtensions
{
    /// <summary>
    /// Configures JWT authentication with strong security validation and authorization.
    /// </summary>
    /// <param name="builder">The web application builder</param>
    public static void ConfigureJwtAuthenticationAndAuthorization(this WebApplicationBuilder builder)
    {
        ConfigurationManager config = builder.Configuration;

        // Bind JWT settings from configuration
        config.BindConfigSection(out JwtSettings jwtSettings);

        // Allow JWT secret to be overridden by environment variable for production security
        string jwtSecretKey = config["JWT_SECRET_KEY"] ?? jwtSettings.SecretKey;

        // Validate JWT secret key with strong cryptographic requirements
        ValidateJwtSecretKey(jwtSecretKey);

        // Configure JWT Bearer authentication
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
                };
            });

        builder.Services.AddAuthorization();
    }

    /// <summary>
    /// Validates that the JWT secret key meets strong cryptographic requirements.
    /// </summary>
    /// <param name="jwtSecretKey">The JWT secret key to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when the key doesn't meet security requirements</exception>
    private static void ValidateJwtSecretKey(string jwtSecretKey)
    {
        // Check if key is provided
        if (string.IsNullOrEmpty(jwtSecretKey))
        {
            throw new InvalidOperationException(
                "JWT Secret Key is required. " +
                "Set it via 'JWT_SECRET_KEY' environment variable or 'JwtSettings:SecretKey' in appsettings.");
        }

        // Check minimum length (64 characters for production)
        if (jwtSecretKey.Length < 64)
        {
            throw new InvalidOperationException(
                $"JWT Secret Key must be at least 64 characters long for production use (current length: {jwtSecretKey.Length}). " +
                "Set a secure random key via 'JWT_SECRET_KEY' environment variable.");
        }

        // Check for placeholder values
        if (jwtSecretKey.Contains("REPLACE", StringComparison.OrdinalIgnoreCase) ||
            jwtSecretKey.Contains("change-this", StringComparison.OrdinalIgnoreCase) ||
            jwtSecretKey.Contains("example", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "JWT Secret Key contains placeholder text and has not been properly configured. " +
                "Set a secure random key via 'JWT_SECRET_KEY' environment variable.");
        }

        // Check for sufficient entropy (at least 16 distinct characters for 64+ char key)
        int distinctChars = jwtSecretKey.Distinct().Count();
        if (distinctChars < 16)
        {
            throw new InvalidOperationException(
                $"JWT Secret Key must be cryptographically random with sufficient entropy " +
                $"(found only {distinctChars} distinct characters, need at least 16). " +
                "Generate a secure random key using a cryptographic random number generator.");
        }

        // Warn if key appears to have low complexity (too many repeated characters)
        IGrouping<char, char> charGroups = jwtSecretKey.GroupBy(c => c).OrderByDescending(g => g.Count()).First();
        if (charGroups.Count() > jwtSecretKey.Length / 4)
        {
            throw new InvalidOperationException(
                $"JWT Secret Key has insufficient randomness (character '{charGroups.Key}' appears {charGroups.Count()} times). " +
                "Generate a secure random key using a cryptographic random number generator.");
        }
    }
}
