namespace ClaudePlayground.Domain.Common;

/// <summary>
/// Represents an error that occurred during an operation
/// </summary>
public sealed class Error
{
    /// <summary>
    /// A unique code identifying the error type
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// A human-readable description of the error
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The type/category of the error
    /// </summary>
    public ErrorType Type { get; }

    public Error(string code, string message, ErrorType type = ErrorType.Failure)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    /// <summary>
    /// Predefined error for when an entity is not found
    /// </summary>
    public static Error NotFound(string entityName, string id) =>
        new($"{entityName}.NotFound", $"{entityName} with ID '{id}' was not found", ErrorType.NotFound);

    /// <summary>
    /// Predefined error for validation failures
    /// </summary>
    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    /// <summary>
    /// Predefined error for conflicts (e.g., duplicate entries)
    /// </summary>
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Predefined error for unauthorized access
    /// </summary>
    public static Error Unauthorized(string message) =>
        new("Unauthorized", message, ErrorType.Unauthorized);

    /// <summary>
    /// Predefined error for forbidden access
    /// </summary>
    public static Error Forbidden(string message) =>
        new("Forbidden", message, ErrorType.Forbidden);

    /// <summary>
    /// General failure error
    /// </summary>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}

/// <summary>
/// Categorizes errors by type for proper HTTP status code mapping
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
