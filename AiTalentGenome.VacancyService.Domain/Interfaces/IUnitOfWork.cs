namespace AiTalentGenome.VacancyService.Domain.Interfaces;

public interface IUnitOfWork
{
    IVacancyRepository Vacancies { get; }
    IApplicationRepository Applications { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}