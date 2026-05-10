using AiTalentGenome.VacancyService.Application.DTOs.External;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Handlers;

public class SyncVacanciesHandler(
    IUnitOfWork unitOfWork, 
    IHeadHunterService hhService,
    IServiceScopeFactory scopeFactory) : IRequestHandler<SyncVacanciesCommand, int>
{
    public async Task<int> Handle(SyncVacanciesCommand request, CancellationToken ct)
    {
        // 1. Получаем список "поверхностных" вакансий
        var briefVacancies = await hhService.GetActiveVacanciesAsync(request.AccessToken, ct);
        if (!briefVacancies.Any()) return 0;

        int syncedCount = 0;
        // Ограничиваем параллелизм до 5 запросов, чтобы HH не выдал 429 (Too Many Requests)
        var semaphore = new SemaphoreSlim(5);
    
        var tasks = briefVacancies.Select(async brief =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                // СОЗДАЕМ НОВЫЙ SCOPE ДЛЯ КАЖДОЙ ЗАДАЧИ
                using var scope = scopeFactory.CreateScope();
                // Достаем UnitOfWork именно из этого scope
                var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var fullInfo = await hhService.GetVacancyDetailsAsync(request.AccessToken, brief.Id, ct);
                if (fullInfo == null) return;

                var existingVacancy = await scopedUnitOfWork.Vacancies.GetByHhIdAsync(fullInfo.Id, ct);

                if (existingVacancy != null)
                {
                    UpdateVacancyFields(existingVacancy, fullInfo);
                    scopedUnitOfWork.Vacancies.Update(existingVacancy);
                }
                else
                {
                    var newVacancy = new Vacancy
                    {
                        Id = Guid.NewGuid(),
                        HhId = fullInfo.Id,
                        OwnerId = request.UserId,
                        CompanyId = request.CompanyId,
                        CreatedAt = DateTime.UtcNow
                    };
                    UpdateVacancyFields(newVacancy, fullInfo);
                    await scopedUnitOfWork.Vacancies.AddAsync(newVacancy, ct);
                }

                // Сохраняем изменения сразу для этого потока
                await scopedUnitOfWork.SaveChangesAsync(ct);
                Interlocked.Increment(ref syncedCount);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // 3. Сохраняем всё одним махом
        await unitOfWork.SaveChangesAsync(ct);
        return syncedCount;
    }

    private void UpdateVacancyFields(Vacancy vacancy, HhVacancyDto external)
    {
        vacancy.Title = external.Name;
        vacancy.Description = external.Description ?? string.Empty;
        vacancy.AreaName = external.Area?.Name;
        vacancy.Experience = external.Experience?.Name;
        vacancy.KeySkills = external.KeySkills?.Select(s => s.Name).ToList() ?? new();
        
        if (external.Salary != null)
        {
            vacancy.Salary = new Domain.ValueObjects.Salary(
                external.Salary.From, 
                external.Salary.To, 
                external.Salary.Currency);
        }
    }
}