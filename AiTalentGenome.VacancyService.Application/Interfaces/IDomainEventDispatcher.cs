using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Domain.Common;

namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface IDomainEventDispatcher
{
    Task<DomainDispatchResult> DispatchAsync(CancellationToken ct = default);
}