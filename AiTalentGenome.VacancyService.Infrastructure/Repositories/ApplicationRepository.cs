using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTalentGenome.VacancyService.Infrastructure.Repositories;

public class ApplicationRepository(VacancyDbContext context) : IApplicationRepository
{
    public async Task AddAsync(Domain.Entities.Application application, CancellationToken ct = default)
    {
        await context.Applications.AddAsync(application, ct);
    }

    public async Task<IEnumerable<Domain.Entities.Application>> GetByVacancyIdAsync(Guid vacancyId, CancellationToken ct = default)
    {
        return await context.Applications
            .Where(a => a.VacancyId == vacancyId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync(ct);
    }
}