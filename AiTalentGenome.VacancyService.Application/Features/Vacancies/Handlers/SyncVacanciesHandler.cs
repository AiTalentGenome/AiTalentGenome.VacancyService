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
        // 1. Получаем данные из внешнего API
        var externalVacancies = await hhService.GetActiveVacanciesAsync(request.AccessToken, ct);
        int syncedCount = 0;

        foreach (var external in externalVacancies)
        {
            // 2. Проверяем, есть ли уже такая вакансия в нашей БД
            var existingVacancy = await unitOfWork.Vacancies.GetByHhIdAsync(external.Id, ct);

            if (existingVacancy != null)
            {
                // Обновляем существующую
                UpdateVacancyFields(existingVacancy, external);
                unitOfWork.Vacancies.Update(existingVacancy);
            }
            else
            {
                // Создаем новую
                var newVacancy = new Vacancy
                {
                    Id = Guid.NewGuid(),
                    HhId = external.Id,
                    OwnerId = request.UserId,
                    CompanyId = request.CompanyId,
                    CreatedAt = DateTime.UtcNow
                };
                UpdateVacancyFields(newVacancy, external);
                await unitOfWork.Vacancies.AddAsync(newVacancy, ct);
            }
            syncedCount++;
        }

        // 3. Сохраняем все изменения одной транзакцией
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