using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;

public record SyncVacanciesCommand(string AccessToken, long UserId, long CompanyId) : IRequest<int>;