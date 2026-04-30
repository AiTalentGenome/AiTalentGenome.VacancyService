using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Queries;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Handlers;

public class GetVacancyByIdHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetVacancyByIdQuery, VacancyDetailDto?>
{
    public async Task<VacancyDetailDto?> Handle(GetVacancyByIdQuery request, CancellationToken ct)
    {
        var vacancy = await unitOfWork.Vacancies.GetByIdAsync(request.Id, ct);

        if (vacancy == null) return null;

        return new VacancyDetailDto(
            vacancy.Id,
            vacancy.Title,
            vacancy.Description,
            vacancy.KeySkills,
            vacancy.Salary != null ? new SalaryDto(vacancy.Salary.From, vacancy.Salary.To, vacancy.Salary.Currency) : null,
            vacancy.Experience,
            vacancy.AreaName
        );
    }
}