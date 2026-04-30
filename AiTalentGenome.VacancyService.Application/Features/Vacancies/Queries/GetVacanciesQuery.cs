using AiTalentGenome.VacancyService.Application.DTOs;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Queries;

public record GetVacanciesQuery(bool OnlyActive) : IRequest<List<VacancyShortDto>>;