using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Features.Applications.Queries;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Handlers;

public class GetPagedApplicationsByVacancyHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetPagedApplicationsByVacancyQuery, PagedApplicationsResponseDto>
{
    public async Task<PagedApplicationsResponseDto> Handle(GetPagedApplicationsByVacancyQuery request, CancellationToken ct)
    {
        var (apps, totalCount) = await unitOfWork.Applications.GetPagedFilteredAsync(
            request.VacancyId, 
            request.Page,
            request.PageSize,
            request.Statuses, 
            request.OnlyAnalyzed, 
            ct);

        var dtos = apps.Select(a => new ApplicationResponseDto(
            a.Id,
            a.CandidateName,
            a.CandidateEmail,
            a.LastJobTitle ?? "Не указано",
            a.TotalExperienceMonths ?? 0,
            a.AiScore ?? 0,
            a.Status,
            a.CandidateSkills,
            a.AppliedAt,
            a.AiAnalysisJson
        )).ToList();

        return new PagedApplicationsResponseDto(dtos, totalCount);
    }
}