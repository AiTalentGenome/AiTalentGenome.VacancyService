using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Commands;

public record AddManualCandidateCommand(
    Guid VacancyId,
    string Name,
    string Email,
    string? Phone,
    string? ResumeUrl,
    string? CoverLetter) : IRequest<Guid>;