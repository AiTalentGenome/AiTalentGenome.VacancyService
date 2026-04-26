using AiTalentGenome.VacancyService.Domain.Entities;

namespace AiTalentGenome.VacancyService.Domain.Interfaces;

public interface IVacancyRepository
{
    Task<Vacancy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Vacancy?> GetByHhIdAsync(string hhId, CancellationToken ct = default);
    Task<IEnumerable<Vacancy>> GetAllAsync(bool onlyActive, CancellationToken ct = default);
    Task AddAsync(Vacancy vacancy, CancellationToken ct = default);
    void Update(Vacancy vacancy);
}