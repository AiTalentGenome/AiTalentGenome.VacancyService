using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Enums;
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
    
    public async Task<(List<Domain.Entities.Application> Items, int TotalCount)> GetPagedFilteredAsync(
        Guid vacancyId, 
        int page, 
        int pageSize, 
        List<ApplicationStatus>? statuses, 
        bool? onlyAnalyzed, 
        CancellationToken ct)
    {
        var query = context.Applications
            .AsNoTracking()
            .Where(a => a.VacancyId == vacancyId);

        // Применяем те же фильтры
        if (statuses != null && statuses.Count > 0)
        {
            query = query.Where(a => statuses.Contains(a.Status));
        }

        if (onlyAnalyzed == true)
        {
            query = query.Where(a => a.AiScore != null);
        }

        // 1. Считаем общее количество подходящих записей в БД (до пагинации)
        var totalCount = await query.CountAsync(ct);

        // 2. Делаем сортировку и выбираем нужный кусок (страницу) данных
        var items = await query
            .OrderByDescending(a => a.AiScore) // Сначала лучшие по AI-скору
            .ThenByDescending(a => a.AppliedAt) // Затем самые свежие
            .Skip((page - 1) * pageSize)       // Пропускаем предыдущие страницы
            .Take(pageSize)                    // Берем размер страницы
            .ToListAsync(ct);

        // Возвращаем именованный кортеж
        return (Items: items, TotalCount: totalCount);
    }
    
    public async Task<Domain.Entities.Application?> GetByIdAsync(Guid id, CancellationToken ct = default) 
        => await context.Applications
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    // Получение всех откликов на конкретную вакансию
    public async Task<List<Domain.Entities.Application>> GetByVacancyIdAsync(Guid vacancyId, CancellationToken ct = default)
    {
        return await context.Applications
            .Where(a => a.VacancyId == vacancyId)
            .OrderByDescending(a => a.AppliedAt) // Сначала свежие отклики
            .ToListAsync(ct);
    }
    
    public async Task<List<Domain.Entities.Application>> GetFilteredAsync(
        Guid vacancyId, 
        List<ApplicationStatus>? statuses, 
        bool? onlyAnalyzed, 
        CancellationToken ct)
    {
        var query = context.Applications
            .AsNoTracking()
            .Where(a => a.VacancyId == vacancyId);

        // Фильтр по статусам (чекбоксы)
        if (statuses != null && statuses.Count > 0)
        {
            query = query.Where(a => statuses.Contains(a.Status));
        }

        // Фильтр "Проанализированные" (те, у кого уже есть оценка AI)
        if (onlyAnalyzed == true)
        {
            query = query.Where(a => a.AiScore != null);
        }

        return await query
            .OrderByDescending(a => a.AiScore) // Сначала лучшие кандидаты
            .ThenByDescending(a => a.AppliedAt)
            .ToListAsync(ct);
    }

    // Обновление данных (например, статуса или AI-оценки)
    public void Update(Domain.Entities.Application application)
    {
        context.Applications.Update(application);
    }
}