using AiTalentGenome.VacancyService.Application.DTOs.External;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Handlers;

public class SyncVacanciesHandler(
    IUnitOfWork unitOfWork, 
    IHeadHunterService hhService) : IRequestHandler<SyncVacanciesCommand, int>
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
                // 2. Для каждой вакансии тянем ПОЛНЫЕ данные
                var fullInfo = await hhService.GetVacancyDetailsAsync(request.AccessToken, brief.Id, ct);
                if (fullInfo == null) return;

                var existingVacancy = await unitOfWork.Vacancies.GetByHhIdAsync(fullInfo.Id, ct);

                if (existingVacancy != null)
                {
                    UpdateVacancyFields(existingVacancy, fullInfo);
                    unitOfWork.Vacancies.Update(existingVacancy);
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
                    await unitOfWork.Vacancies.AddAsync(newVacancy, ct);
                }
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