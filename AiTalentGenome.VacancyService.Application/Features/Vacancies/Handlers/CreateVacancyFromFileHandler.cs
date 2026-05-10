using AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using AiTalentGenome.VacancyService.Domain.ValueObjects;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Handlers;

public class CreateVacancyFromFileHandler(IDocumentParserClient parserClient, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVacancyFromFileCommand, Guid>
{
    public async Task<Guid> Handle(CreateVacancyFromFileCommand request, CancellationToken ct)
    {
        // 1. Запрос к микросервису парсинга через gRPC клиент
        var parsedData = await parserClient.ParseVacancyAsync(request.FileBytes, request.Extension, ct);

        // 2. Инициализация сущности на основе полученных от AI данных
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = parsedData.Title,
            Description = parsedData.Description,
            KeySkills = parsedData.KeySkills.ToList(),
            Experience = parsedData.Experience,
            AreaName = parsedData.AreaName,
            OwnerId = request.OwnerId,
            CompanyId = request.CompanyId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Salary = new Salary(From: parsedData.SalaryFrom, To: parsedData.SalaryTo, Currency: "KZT"),
        };

        await unitOfWork.Vacancies.AddAsync(vacancy, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return vacancy.Id;
    }
}