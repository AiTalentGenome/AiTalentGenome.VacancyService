using AiTalentGenome.VacancyService.Application.DTOs;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Queries;

public record GetVacancyByIdQuery(Guid Id) : IRequest<VacancyDetailDto?>;