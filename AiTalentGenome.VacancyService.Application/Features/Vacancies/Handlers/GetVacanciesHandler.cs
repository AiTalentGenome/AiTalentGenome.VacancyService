using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Queries;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Handlers;

public class GetVacanciesHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetVacanciesQuery, List<VacancyShortDto>>
{
    public async Task<List<VacancyShortDto>> Handle(GetVacanciesQuery request, CancellationToken ct)
    {
        // 1. Получаем модели из Domain
        var summaries = await unitOfWork.Vacancies.GetSummariesAsync(request.OnlyActive, ct);

        // 2. Превращаем их в DTO (здесь можно добавить название компании и т.д.)
        return summaries.Select(s => new VacancyShortDto(
            s.Id,
            s.HhId,
            s.Title,
            "Ваша компания", 
            s.CreatedAt,
            s.AreaName ?? "Не указан", 
            s.IsActive,
            s.ApplicationsCount
        )).ToList();
    }
}