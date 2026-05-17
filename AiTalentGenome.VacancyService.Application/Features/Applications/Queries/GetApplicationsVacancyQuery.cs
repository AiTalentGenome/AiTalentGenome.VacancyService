using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Domain.Enums;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Queries;

public record GetApplicationsByVacancyQuery(
    Guid VacancyId, 
    List<ApplicationStatus>? Statuses = null, 
    bool? OnlyAnalyzed = null) : IRequest<List<ApplicationResponseDto>>;