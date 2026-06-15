using AiTalentGenome.Contracts.Analyzer;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class CandidateAnalysisService(
    IServiceProvider serviceProvider, // Иногда полезно иметь доступ к провайдеру
    IHeadHunterService hhService,
    ILogger<CandidateAnalysisService> logger,
    AnalyzerService.AnalyzerServiceClient grpcClient,
    IUnitOfWork unitOfWork
) : ICandidateAnalysisService
{
    public async Task EnrichCandidateDataAsync(Guid applicationId, string accessToken)
    {
        logger.LogInformation("Запуск обогащения данных для отклика {Id}", applicationId);

        var application = await unitOfWork.Applications.GetByIdAsync(applicationId);
        if (application == null)
        {
            logger.LogWarning("Отклик {Id} не найден в БД", applicationId);
            return;
        }

        if (string.IsNullOrEmpty(application.HhResumeId) || application.HhResumeId == "no_id")
        {
            logger.LogWarning("У отклика {Id} отсутствует корректный HhResumeId.", applicationId);
            return;
        }

        try
        {
            // 1. Получаем полную инфоструктуру резюме
            var enrichmentResult = await hhService.GetResumeRawTextAsync(accessToken, application.HhResumeId);

            if (enrichmentResult == null || string.IsNullOrEmpty(enrichmentResult.RawText))
            {
                logger.LogWarning("Не удалось собрать метаданные резюме для отклика {Id}", applicationId);
                return;
            }

            // Заполняем текстовые блоки и метаданные из HH
            application.RawResumeText = enrichmentResult.RawText;
            application.Education = enrichmentResult.Education ?? "Не указано";
            application.LastJobTitle = enrichmentResult.LastJobTitle ?? "Не указана";
            application.LastCompany = enrichmentResult.LastCompany ?? "Не указана";
            application.TotalExperienceMonths = enrichmentResult.TotalExperienceMonths ?? 0;

            // 2. Скачиваем сопроводительное письмо
            if (string.IsNullOrEmpty(application.CoverLetter) && !string.IsNullOrEmpty(application.HhNegotiationId))
            {
                var coverLetter = await hhService.GetCoverLetterAsync(accessToken, application.HhNegotiationId);
                if (!string.IsNullOrEmpty(coverLetter))
                {
                    application.CoverLetter = coverLetter;
                }
            }

            // 3. Обновляем навыки массивом
            var skills = await hhService.GetResumeSkillsAsync(accessToken, application.HhResumeId);
            if (skills != null && skills.Count > 0)
            {
                application.CandidateSkills = skills;
            }

            // 4. ЗАПОЛНЕНИЕ CRITICAL MISMATCHES (Бизнес-логика несоответствий)
            // Получаем вакансию, чтобы сравнить KeySkills с навыками кандидата
            var vacancy = await unitOfWork.Vacancies.GetByIdAsync(application.VacancyId);
            if (vacancy != null && vacancy.KeySkills != null && vacancy.KeySkills.Count > 0)
            {
                var mismatches = new List<string>();
                foreach (var requiredSkill in vacancy.KeySkills)
                {
                    // Проверяем, указан ли жесткий навык в массиве CandidateSkills или общем тексте резюме
                    bool hasSkill =
                        application.CandidateSkills.Any(s =>
                            s.Equals(requiredSkill, StringComparison.OrdinalIgnoreCase))
                        || (application.RawResumeText != null &&
                            application.RawResumeText.Contains(requiredSkill, StringComparison.OrdinalIgnoreCase));

                    if (!hasSkill)
                    {
                        mismatches.Add($"Отсутствует обязательный навык: {requiredSkill}");
                    }
                }

                application.CriticalMismatches = mismatches;
            }

            // 5. Сохраняем обновленную сущность в БД
            unitOfWork.Applications.Update(application);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation(
                "Данные отклика {Id} успешно обогащены. Поля Education, LastJobTitle, LastCompany и CriticalMismatches заполнены.",
                applicationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Критическая ошибка при обогащении данных отклика {Id}", applicationId);
        }
    }

    public async Task<(double AiScore, string AiAnalysisJson, List<string> ExtractedSkills)> AnalyzeApplicationAsync(
        Vacancy vacancy,
        Domain.Entities.Application application,
        string userCriteria,
        CancellationToken cancellationToken = default)
    {
        // 1. Формируем вложенный объект контекста вакансии
        var vacancyContext = new VacancyContext
        {
            Title = vacancy.Title,
            Description = vacancy.Description,
            Experience = vacancy.Experience ?? "Не указан"
        };

        // Переносим KeySkills из List<string> в repeated-поле gRPC контракта
        if (vacancy.KeySkills != null && vacancy.KeySkills.Count > 0)
        {
            vacancyContext.KeySkills.AddRange(vacancy.KeySkills);
        }

        // 2. Собираем финальный gRPC запрос для Python
        var request = new AnalyzeRequest
        {
            Vacancy = vacancyContext,
            ResumeText = application.RawResumeText ?? string.Empty,
            CoverLetter = application.CoverLetter ?? string.Empty,
            UserCriteria = userCriteria ?? string.Empty
        };

        try
        {
            // 3. Выполняем запрос к Python-микросервису (порт :5105)
            var response = await grpcClient.AnalyzeCandidateAsync(request, cancellationToken: cancellationToken);

            return (
                response.AiScore,
                response.AiAnalysisJson,
                response.ExtractedSkills.ToList()
            );
        }
        catch (RpcException ex)
        {
            // Fail-safe: если Ollama зависла или упал Python, возвращаем дефолты, чтобы транзакция в БД не падала
            Console.WriteLine($"[gRPC Error] Python AnalyzerService is unavailable: {ex.Status.Detail}");
            return (0.0, "{}", new List<string>());
        }
    }
}