using ERP.Application.Common.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>
/// One transaction per command, opened and committed by the pipeline.
/// Nested calls join the outer transaction instead of starting a second one,
/// which is what makes composite ERP operations ("post invoice + move stock +
/// write journal entry") atomic without any handler knowing about it.
/// </summary>
public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        // Already inside a transaction (nested command, or the test host) - just run.
        if (context.Database.CurrentTransaction is not null)
            return await operation(cancellationToken);

        // The in-memory provider used by tests has no transaction support.
        if (!context.Database.IsRelational())
            return await operation(cancellationToken);

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
