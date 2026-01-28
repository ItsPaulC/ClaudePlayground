using ClaudePlayground.Application.Interfaces;
using MongoDB.Driver;

namespace ClaudePlayground.Infrastructure.Persistence;

public class MongoTransactionManager : ITransactionManager
{
    private readonly MongoDbContext _context;

    public MongoTransactionManager(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<object, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        using IClientSessionHandle session = await _context.StartSessionAsync(cancellationToken);

        session.StartTransaction();

        try
        {
            TResult result = await operation(session, cancellationToken);

            await session.CommitTransactionAsync(cancellationToken);

            return result;
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }
}
