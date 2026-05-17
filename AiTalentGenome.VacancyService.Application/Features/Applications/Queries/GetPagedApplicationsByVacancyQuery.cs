using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Domain.Enums;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Queries;

public record GetPagedApplicationsByVacancyQuery(
    Guid VacancyId, 
    int Page, 
    int PageSize,
    List<ApplicationStatus>? Statuses = null, 
    bool? OnlyAnalyzed = null) : IRequest<PagedApplicationsResponseDto>;