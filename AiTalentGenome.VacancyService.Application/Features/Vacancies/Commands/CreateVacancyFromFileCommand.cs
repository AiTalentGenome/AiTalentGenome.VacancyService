using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;

public record CreateVacancyFromFileCommand(
    byte[] FileBytes, 
    string Extension, 
    long OwnerId, 
    long CompanyId) : IRequest<Guid>;