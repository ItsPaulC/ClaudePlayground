namespace ClaudePlayground.Application.Interfaces;

/// <summary>
/// Manages database transactions for atomic operations across multiple repository calls
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// Executes a function within a database transaction.
    /// If the function completes successfully, the transaction is committed.
    /// If an exception is thrown, the transaction is aborted and rolled back.
    /// </summary>
    /// <typeparam name="TResult">The return type of the function</typeparam>
    /// <param name="operation">The function to execute within the transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the function</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<object, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
