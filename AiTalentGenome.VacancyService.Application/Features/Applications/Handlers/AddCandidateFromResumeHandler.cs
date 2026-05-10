using AiTalentGenome.VacancyService.Application.Features.Applications.Commands;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Enums;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Handlers;

public class AddCandidateFromResumeHandler(IDocumentParserClient parserClient, IUnitOfWork unitOfWork)
    : IRequestHandler<AddCandidateFromResumeCommand, Guid>
{
    public async Task<Guid> Handle(AddCandidateFromResumeCommand request, CancellationToken ct)
    {
        // 1. Сначала получаем данные вакансии из БД
        var vacancy = await unitOfWork.Vacancies.GetByIdAsync(request.VacancyId);
        if (vacancy == null) throw new Exception("Vacancy not found");

        // 2. Теперь отправляем запрос парсеру с контекстом вакансии
        var resume = await parserClient.ParseResumeAsync(
            request.FileBytes, 
            request.Extension,
            vacancy.Title,
            vacancy.Description,
            vacancy.KeySkills,
            ct
        );

        // 3. Создаем Application со всеми новыми полями
        var application = new Domain.Entities.Application
        {
            Id = Guid.NewGuid(),
            VacancyId = request.VacancyId,
            CandidateName = resume.CandidateName,
            CandidateEmail = resume.CandidateEmail,
            CandidatePhone = resume.CandidatePhone,
            CoverLetter = resume.CoverLetter,
            AiScore = resume.AiScore,
            AiSummary = resume.AiAnalysisJson, // Сохраняем как JSON или текст
            RawResumeText = resume.RawResumeText,
            CandidateSkills = resume.CandidateSkills.ToList(),
            TotalExperienceMonths = resume.TotalExperienceMonths,
            LastJobTitle = resume.LastJobTitle,
            LastCompany = resume.LastCompany,
            Education = resume.Education,
            CriticalMismatches = resume.CriticalMismatches.ToList(),
            Status = ApplicationStatus.Submitted
        };

        await unitOfWork.Applications.AddAsync(application, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return application.Id;
    }
}