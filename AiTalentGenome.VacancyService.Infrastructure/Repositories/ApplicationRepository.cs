using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTalentGenome.VacancyService.Infrastructure.Repositories;

public class ApplicationRepository(VacancyDbContext context) : IApplicationRepository
{
    // Добавление нового отклика
    public async Task AddAsync(Domain.Entities.Application application, CancellationToken ct = default)
    {
        await context.Applications.AddAsync(application, ct);
    }

    // Поиск отклика по ID из HeadHunter
    public async Task<Domain.Entities.Application?> GetByHhIdAsync(string hhId, CancellationToken ct = default)
    {
        return await context.Applications
            .FirstOrDefaultAsync(a => a.HhNegotiationId == hhId, ct);
    }

    // Получение всех откликов на конкретную вакансию
    public async Task<List<Domain.Entities.Application>> GetByVacancyIdAsync(Guid vacancyId, CancellationToken ct = default)
    {
        return await context.Applications
            .Where(a => a.VacancyId == vacancyId)
            .OrderByDescending(a => a.AppliedAt) // Сначала свежие отклики
            .ToListAsync(ct);
    }

    // Обновление данных (например, статуса или AI-оценки)
    public void Update(Domain.Entities.Application application)
    {
        context.Applications.Update(application);
    }
}