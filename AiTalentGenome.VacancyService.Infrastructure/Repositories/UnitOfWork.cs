using AiTalentGenome.VacancyService.Domain.Interfaces;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;

namespace AiTalentGenome.VacancyService.Infrastructure.Repositories;

public class UnitOfWork(
    VacancyDbContext context,
    IVacancyRepository vacancies,
    IApplicationRepository applications) : IUnitOfWork
{
    public IVacancyRepository Vacancies => vacancies;
    public IApplicationRepository Applications => applications;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) 
        => await context.SaveChangesAsync(ct);
}