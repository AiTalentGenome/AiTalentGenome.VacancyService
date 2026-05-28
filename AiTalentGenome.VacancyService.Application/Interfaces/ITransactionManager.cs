using AiTalentGenome.VacancyService.Application.DTOs;

namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface ITransactionManager
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task<TransactionResult> CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}