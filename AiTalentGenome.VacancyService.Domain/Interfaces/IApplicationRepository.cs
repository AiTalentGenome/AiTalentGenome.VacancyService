using AiTalentGenome.VacancyService.Domain.Entities;

namespace AiTalentGenome.VacancyService.Domain.Interfaces;

public interface IApplicationRepository
{
    Task AddAsync(Application application, CancellationToken ct = default);
    Task<IEnumerable<Application>> GetByVacancyIdAsync(Guid vacancyId, CancellationToken ct = default);
}