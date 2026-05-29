using AiTalentGenome.VacancyService.Application.DTOs;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;

public record StartAiAnalysisCommand(
    Guid VacancyId, 
    List<Guid> ApplicationIds, 
    string UserCriteria,
    string AccessToken // Добавили поле
) : IRequest<List<AnalyzedCandidateResultDto>>;