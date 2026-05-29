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

        // 1. Берем отклик из базы
        var application = await unitOfWork.Applications.GetByIdAsync(applicationId);
        if (application == null)
        {
            logger.LogWarning("Отклик {Id} не найден в БД", applicationId);
            return;
        }

        if (string.IsNullOrEmpty(application.HhResumeId) || application.HhResumeId == "no_id")
        {
            logger.LogWarning("У отклика {Id} отсутствует корректный HhResumeId. Обогащение через HH невозможно.", applicationId);
            return;
        }

        try
        {
            // 2. Получаем полный Raw Текст резюме для AI
            var rawText = await hhService.GetResumeRawTextAsync(accessToken, application.HhResumeId);
            
            if (string.IsNullOrEmpty(rawText))
            {
                logger.LogWarning("Не удалось собрать RawResumeText для отклика {Id}", applicationId);
                return;
            }

            application.RawResumeText = rawText;

            // 3. Заполняем сопроводительное письмо, если его еще нет
            if (string.IsNullOrEmpty(application.CoverLetter) && !string.IsNullOrEmpty(application.HhNegotiationId))
            {
                var coverLetter = await hhService.GetCoverLetterAsync(accessToken, application.HhNegotiationId);
                if (!string.IsNullOrEmpty(coverLetter))
                {
                    application.CoverLetter = coverLetter;
                }
            }

            // 4. Заполняем CandidateSkills (в БД это List<string>)
            var skills = await hhService.GetResumeSkillsAsync(accessToken, application.HhResumeId);
            if (skills != null && skills.Count > 0)
            {
                application.CandidateSkills = skills;
            }

            // 5. Опционально: вытягиваем метаданные для быстрого отображения в UI
            // Чтобы не дергать API повторно, можно было бы распарсить JSON прямо тут, 
            // но для базового обогащения текста этого уже достаточно.

            // 6. Сохраняем изменения
            unitOfWork.Applications.Update(application);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("Данные отклика {Id} успешно обогащены. RawResumeText сохранен.", applicationId);

            // TODO: Шаг 7. Передача на глубокий анализ в AI-сервис (AnalyzerService в Python или локальный LLM через Ollama)
            // Например: Вызов публикации в RabbitMQ или добавление новой задачи в Hangfire.
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