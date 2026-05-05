using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Commands;

public record SyncApplicationsCommand(Guid VacancyId, string HhVacancyId, string AccessToken) : IRequest<int>;