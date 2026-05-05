using AiTalentGenome.VacancyService.Application.Features.Applications.Commands;
using AiTalentGenome.VacancyService.Domain.Enums;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Handlers;

public class AddManualCandidateHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<AddManualCandidateCommand, Guid>
{
    public async Task<Guid> Handle(AddManualCandidateCommand request, CancellationToken ct)
    {
        // 1. Проверяем, существует ли вакансия
        var vacancy = await unitOfWork.Vacancies.GetByIdAsync(request.VacancyId, ct);
        if (vacancy == null)
            throw new KeyNotFoundException("Vacancy not found");

        // 2. Создаем отклик
        var application = new Domain.Entities.Application
        {
            Id = Guid.NewGuid(),
            VacancyId = request.VacancyId,
            CandidateName = request.Name,
            CandidateEmail = request.Email,
            CandidatePhone = request.Phone,
            ResumeUrl = request.ResumeUrl,
            CoverLetter = request.CoverLetter,
            Status = ApplicationStatus.Submitted,
            AppliedAt = DateTime.UtcNow
        };

        await unitOfWork.Applications.AddAsync(application, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return application.Id;
    }
}