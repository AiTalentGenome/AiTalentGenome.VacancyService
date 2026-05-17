using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Features.Applications.Queries;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Handlers;

public class GetApplicationsByVacancyHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetApplicationsByVacancyQuery, List<ApplicationResponseDto>>
{
    public async Task<List<ApplicationResponseDto>> Handle(GetApplicationsByVacancyQuery request, CancellationToken ct)
    {
        // Вызываем оптимизированный репозиторий
        var apps = await unitOfWork.Applications.GetFilteredAsync(
            request.VacancyId, 
            request.Statuses, 
            request.OnlyAnalyzed, 
            ct);

        return apps.Select(a => new ApplicationResponseDto(
            a.Id,
            a.CandidateName,
            a.CandidateEmail,
            a.LastJobTitle ?? "Не указано",
            a.TotalExperienceMonths ?? 0,
            a.AiScore ?? 0,
            a.Status,
            a.CandidateSkills,
            a.AppliedAt
        )).ToList();
    }
}