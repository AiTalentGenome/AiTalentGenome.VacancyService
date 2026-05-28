using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class TransactionManager(
    VacancyDbContext context,
    ILogger<TransactionManager> logger, 
    IDomainEventDispatcher domainEventDispatcher,
    IOutboxService outbox,
    IDbContextTransaction? currentTransaction
) : ITransactionManager, IDisposable
{
    private IDbContextTransaction? _currentTransaction = currentTransaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await context.Database.BeginTransactionAsync();
    }

    public async Task<TransactionResult> CommitTransactionAsync(CancellationToken ct = default)
    {
        if (currentTransaction == null)
        {
            return new TransactionResult(false, "Транзакция не была инициализирована.");
        }

        try
        {
            var dispatchResult = await domainEventDispatcher.DispatchAsync(ct);

            if (!dispatchResult.IsSuccess)
            {
                await RollbackAsync(ct);
                return new TransactionResult(false, dispatchResult.ErrorMessage);
            }
            await context.SaveChangesAsync(ct);
            await currentTransaction.CommitAsync(ct);
            await outbox.FlushOutgoingMessagesAsync(ct);
            return new TransactionResult(true);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Конфликт параллелизма (Concurrency conflict) при коммите.");
            await RollbackAsync(ct);
            return new TransactionResult(false, "Конфликт параллелизма.");
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при откатe транзакции.");
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }
    
    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        context.Dispose();
    }
}