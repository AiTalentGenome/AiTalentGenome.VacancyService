using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class CandidateAnalysisService(
    IServiceProvider serviceProvider, // Иногда полезно иметь доступ к провайдеру
    IHeadHunterService hhService,
    ILogger<CandidateAnalysisService> logger
) : ICandidateAnalysisService
{
    public async Task EnrichCandidateDataAsync(Guid applicationId, string accessToken)
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var app = await unitOfWork.Applications.GetByIdAsync(applicationId);

        // Проверяем наличие необходимых ID для работы
        if (app == null || string.IsNullOrEmpty(app.HhResumeId) || string.IsNullOrEmpty(app.HhNegotiationId)) return;

        // Небольшая задержка, чтобы не спамить API HH слишком быстро
        await Task.Delay(1000);

        // 1. Получаем навыки
        var fullSkills = await hhService.GetResumeSkillsAsync(accessToken, app.HhResumeId);
        app.CandidateSkills = fullSkills;

        // 2. Получаем сопроводительное письмо (Cover Letter)
        var coverLetter = await hhService.GetCoverLetterAsync(accessToken, app.HhNegotiationId);
        app.CoverLetter = coverLetter;

        // 3. Сохраняем всё вместе
        unitOfWork.Applications.Update(app);
        await unitOfWork.SaveChangesAsync();
    
        logger.LogInformation("Обогащение данных завершено для кандидата: {Name}", app.CandidateName);
    }
}