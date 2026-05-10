using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Commands;

public record AddCandidateFromResumeCommand(
    Guid VacancyId, 
    byte[] FileBytes, 
    string Extension) : IRequest<Guid>;