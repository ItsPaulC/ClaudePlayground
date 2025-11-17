namespace ClaudePlayground.Domain.Common;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error
/// </summary>
/// <typeparam name="T">The type of the value when the operation succeeds</typeparam>
public class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value if the operation was successful
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result</exception>
    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot access Value of a failed result. Check IsSuccess before accessing Value.");
            }
            return _value!;
        }
    }

    /// <summary>
    /// Gets the error if the operation failed
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Error on a successful result</exception>
    public Error Error
    {
        get
        {
            if (IsSuccess)
            {
                throw new InvalidOperationException("Cannot access Error of a successful result. Check IsFailure before accessing Error.");
            }
            return _error!;
        }
    }

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result with the specified value
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Implicit conversion from T to Result&lt;T&gt; for success cases
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion from Error to Result&lt;T&gt; for failure cases
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Executes one of two functions depending on whether the result is a success or failure
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}

/// <summary>
/// Represents the result of an operation that doesn't return a value
/// </summary>
public class Result
{
    private readonly Error? _error;

    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the operation failed
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Error on a successful result</exception>
    public Error Error
    {
        get
        {
            if (IsSuccess)
            {
                throw new InvalidOperationException("Cannot access Error of a successful result. Check IsFailure before accessing Error.");
            }
            return _error!;
        }
    }

    private Result(bool isSuccess, Error? error = null)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the specified error
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Implicit conversion from Error to Result for failure cases
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>
    /// Executes one of two functions depending on whether the result is a success or failure
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}
