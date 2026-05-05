using AiTalentGenome.VacancyService.Application.Features.Applications.Commands;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Enums;
using AiTalentGenome.VacancyService.Domain.Interfaces;
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
        int newCount = 0;

        foreach (var ext in externalApps)
        {
            var existing = await unitOfWork.Applications.GetByHhIdAsync(ext.NegotiationId, ct);
            
            if (existing == null)
            {
                var application = new Domain.Entities.Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = request.VacancyId,
                    HhNegotiationId = ext.NegotiationId,
                    CandidateName = ext.FullName,
                    ResumeUrl = ext.ResumeUrl,
                    Status = MapHhStateToStatus(ext.StateId),
                    AppliedAt = DateTime.UtcNow
                };

                await unitOfWork.Applications.AddAsync(application, ct);
                newCount++;
            }
            else
            {
                // Обновляем статус, если он изменился в HH
                existing.Status = MapHhStateToStatus(ext.StateId);
                unitOfWork.Applications.Update(existing);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return newCount;
    }

    private ApplicationStatus MapHhStateToStatus(string? hhState) => hhState switch
    {
        "inbox" => ApplicationStatus.Submitted,
        "consider" => ApplicationStatus.Screening,
        "phone_interview" or "interview" => ApplicationStatus.Interview,
        "offer" => ApplicationStatus.Offered,
        "discard" => ApplicationStatus.Rejected,
        _ => ApplicationStatus.Submitted
    };
}