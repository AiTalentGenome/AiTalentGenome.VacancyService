using AiTalentGenome.VacancyService.Application.Features.Applications.Commands;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Enums;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Application.Features.Applications.Handlers;

public class SyncApplicationsHandler(
    IUnitOfWork unitOfWork,
    IHeadHunterService hhService,
    ILogger<SyncApplicationsHandler> logger) : IRequestHandler<SyncApplicationsCommand, int>
{
    public async Task<int> Handle(SyncApplicationsCommand request, CancellationToken ct)
    {
        var externalApps = await hhService.GetApplicationsByVacancyAsync(request.AccessToken, request.HhVacancyId, ct);
        int updatedCount = 0;
        
        var appsToEnrich = new List<Guid>();

        foreach (var ext in externalApps)
        {
            var existing = await unitOfWork.Applications.GetByHhIdAsync(ext.NegotiationId, ct);

            if (existing == null)
            {
                // Код создания новой записи (уже работает)
                var application = new Domain.Entities.Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = request.VacancyId,
                    HhNegotiationId = ext.NegotiationId,
                    CandidateName = ext.FullName,
                    CandidateEmail = ext.Email ?? "Не указан",
                    CandidatePhone = ext.Phone,
                    HhResumeId = ext.ResumeId,
                    ResumeUrl = ext.ResumeUrl,
                    Status = MapHhStateToStatus(ext.StateId),
                    AppliedAt = DateTime.UtcNow,
                    CoverLetter = ext.CoverLetter,
                    CandidateSkills = ext.Skills ?? new List<string>()
                };

                await unitOfWork.Applications.AddAsync(application, ct);
                
                appsToEnrich.Add(application.Id); // Запоминаем ID
                updatedCount++;
            }
            else
            {
                existing.Status = MapHhStateToStatus(ext.StateId);
                if (string.IsNullOrEmpty(existing.CoverLetter) || existing.CandidateSkills.Count == 0)
                {
                    appsToEnrich.Add(existing.Id); // Запоминаем ID
                }
                unitOfWork.Applications.Update(existing);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        
        foreach (var appId in appsToEnrich)
        {
            BackgroundJob.Enqueue<ICandidateAnalysisService>(service => 
                service.EnrichCandidateDataAsync(appId, request.AccessToken));
        }
        
        return updatedCount;
    }

    private ApplicationStatus MapHhStateToStatus(string? hhState) => hhState switch
    {
        "inbox" => ApplicationStatus.Submitted, // Неразобранные
        "consider" => ApplicationStatus.Screening, // Подумать
        "phone_interview" => ApplicationStatus.PhoneInterview, // Первичный контакт
        "assessment" => ApplicationStatus.Assessment, // Тестовое задание
        "interview" => ApplicationStatus.Interview, // Собеседование
        "offer" => ApplicationStatus.Offered, // Предложение о работе
        "hired" => ApplicationStatus.Hired, // Выход на работу
        "discard" => ApplicationStatus.Rejected, // Не подходит
        _ => ApplicationStatus.Submitted // По умолчанию
    };
}