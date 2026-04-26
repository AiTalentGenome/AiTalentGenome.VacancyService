using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTalentGenome.VacancyService.Infrastructure.Repositories;

public class VacancyRepository(VacancyDbContext context) : IVacancyRepository
{
    public async Task<Vacancy?> GetByIdAsync(Guid id, CancellationToken ct = default) 
        => await context.Vacancies.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<Vacancy?> GetByHhIdAsync(string hhId, CancellationToken ct = default)
        => await context.Vacancies.FirstOrDefaultAsync(v => v.HhId == hhId, ct);

    public async Task<IEnumerable<Vacancy>> GetAllAsync(bool onlyActive, CancellationToken ct = default)
    {
        var query = context.Vacancies.AsQueryable();
        if (onlyActive) query = query.Where(v => v.IsActive);
        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(Vacancy vacancy, CancellationToken ct = default) 
        => await context.Vacancies.AddAsync(vacancy, ct);

    public void Update(Vacancy vacancy) 
        => context.Vacancies.Update(vacancy);
}