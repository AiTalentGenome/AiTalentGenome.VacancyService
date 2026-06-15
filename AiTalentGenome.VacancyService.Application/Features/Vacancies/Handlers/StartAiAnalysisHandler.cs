using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Handlers;

public class StartAiAnalysisHandler : IRequestHandler<StartAiAnalysisCommand, List<AnalyzedCandidateResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICandidateAnalysisService _analysisService;
    private readonly IHeadHunterService _hhService;

    public StartAiAnalysisHandler(
        IUnitOfWork unitOfWork, 
        ICandidateAnalysisService analysisService, 
        IHeadHunterService hhService)
    {
        _unitOfWork = unitOfWork;
        _analysisService = analysisService;
        _hhService = hhService;
    }

    public async Task<List<AnalyzedCandidateResultDto>> Handle(StartAiAnalysisCommand request, CancellationToken cancellationToken)
    {
        var vacancy = await _unitOfWork.Vacancies.GetByIdAsync(request.VacancyId);
        if (vacancy == null) throw new KeyNotFoundException("Vacancy not found");

        var results = new List<AnalyzedCandidateResultDto>();

        foreach (var appId in request.ApplicationIds)
        {
            var application = await _unitOfWork.Applications.GetByIdAsync(appId);
            if (application == null || application.VacancyId != vacancy.Id) continue;

            // Если текста резюме нет в БД, скачиваем его из HH на лету
            if (string.IsNullOrWhiteSpace(application.RawResumeText) && !string.IsNullOrEmpty(application.HhResumeId))
            {
                Console.WriteLine($"[AI Pre-Processor] Resume text is empty for candidate {application.CandidateName}. Fetching from HH API...");
                
                // ИСПРАВЛЕНИЕ: Получаем объект HhResumeEnrichedResult вместо строки
                var enrichedResult = await _hhService.GetResumeRawTextAsync(request.AccessToken, application.HhResumeId, cancellationToken);
                
                // ИСПРАВЛЕНИЕ: Проверяем свойство RawText внутри объекта
                if (enrichedResult != null && !string.IsNullOrEmpty(enrichedResult.RawText))
                {
                    // Накатываем все полученные метаданные на модель, чтобы они больше не оставались пустыми!
                    application.RawResumeText = enrichedResult.RawText;
                    application.Education = enrichedResult.Education ?? "Не указано";
                    application.LastJobTitle = enrichedResult.LastJobTitle ?? "Не указана";
                    application.LastCompany = enrichedResult.LastCompany ?? "Не указана";
                    application.TotalExperienceMonths = enrichedResult.TotalExperienceMonths ?? 0;
                    
                    if (string.IsNullOrEmpty(application.CoverLetter) && !string.IsNullOrEmpty(application.HhNegotiationId))
                    {
                        application.CoverLetter = await _hhService.GetCoverLetterAsync(request.AccessToken, application.HhNegotiationId, cancellationToken);
                    }
                    
                    // Также подтягиваем навыки из HH, если их массив пуст
                    if (application.CandidateSkills == null || application.CandidateSkills.Count == 0)
                    {
                        var skills = await _hhService.GetResumeSkillsAsync(request.AccessToken, application.HhResumeId, cancellationToken);
                        if (skills != null && skills.Count > 0)
                        {
                            application.CandidateSkills = skills;
                        }
                    }

                    // На лету рассчитываем критические несовпадения перед AI анализом
                    if (vacancy.KeySkills != null && vacancy.KeySkills.Count > 0)
                    {
                        var mismatches = new List<string>();
                        foreach (var requiredSkill in vacancy.KeySkills)
                        {
                            bool hasSkill = (application.CandidateSkills != null && application.CandidateSkills.Any(s => s.Equals(requiredSkill, StringComparison.OrdinalIgnoreCase))) 
                                            || application.RawResumeText.Contains(requiredSkill, StringComparison.OrdinalIgnoreCase);

                            if (!hasSkill)
                            {
                                mismatches.Add($"Отсутствует обязательный навык: {requiredSkill}");
                            }
                        }
                        application.CriticalMismatches = mismatches;
                    }
                    
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            if (string.IsNullOrWhiteSpace(application.RawResumeText))
            {
                Console.WriteLine($"[AI Pre-Processor Warning] Skipping candidate {application.CandidateName}: No resume content available.");
                continue;
            }

            // Вызов Python-микросервиса
            var (aiScore, aiAnalysisJson, extractedSkills) = await _analysisService.AnalyzeApplicationAsync(
                vacancy, 
                application, 
                request.UserCriteria, 
                cancellationToken
            );

            application.AiScore = aiScore;
            application.AiAnalysisJson = aiAnalysisJson;
            
            // Если ИИ извлек более точные навыки, перезаписываем их
            if (extractedSkills != null && extractedSkills.Count > 0)
            {
                application.CandidateSkills = extractedSkills;
            }

            results.Add(new AnalyzedCandidateResultDto(
                application.Id,
                aiScore,
                aiAnalysisJson,
                application.CandidateSkills
            ));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return results;
    }
}