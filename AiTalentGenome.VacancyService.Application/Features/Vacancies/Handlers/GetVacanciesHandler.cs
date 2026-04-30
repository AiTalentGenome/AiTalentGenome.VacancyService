using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Queries;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Handlers;

public class GetVacanciesHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetVacanciesQuery, List<VacancyShortDto>>
{
    public async Task<List<VacancyShortDto>> Handle(GetVacanciesQuery request, CancellationToken ct)
    {
        var vacancies = await unitOfWork.Vacancies.GetAllAsync(request.OnlyActive, ct);

        return vacancies.Select(v => new VacancyShortDto(
            v.Id,
            v.HhId,
            v.Title,
            "Ваша компания", // В будущем можно брать из профиля компании
            v.CreatedAt
        )).ToList();
    }
}