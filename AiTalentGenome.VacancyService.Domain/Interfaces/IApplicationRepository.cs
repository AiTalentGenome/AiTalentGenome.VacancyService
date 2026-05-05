using AiTalentGenome.VacancyService.Domain.Entities;

namespace AiTalentGenome.VacancyService.Domain.Interfaces;

public interface IApplicationRepository
{
    Task AddAsync(Application application, CancellationToken ct = default);
    Task<Application?> GetByHhIdAsync(string hhId, CancellationToken ct = default);
    Task<List<Application>> GetByVacancyIdAsync(Guid vacancyId, CancellationToken ct = default);
    void Update(Application application);
}