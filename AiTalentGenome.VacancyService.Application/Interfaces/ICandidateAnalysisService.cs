using AiTalentGenome.VacancyService.Domain.Entities;

namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface ICandidateAnalysisService
{
    Task EnrichCandidateDataAsync(Guid applicationId, string accessToken);
    
    /// <summary>
    /// Отправляет структурированную вакансию, текст резюме и кастомные критерии 
    /// в Python-микросервис для проведения комплексного ИИ-анализа.
    /// </summary>
    Task<(double AiScore, string AiAnalysisJson, List<string> ExtractedSkills)> AnalyzeApplicationAsync(
        Vacancy vacancy, 
        Domain.Entities.Application application, 
        string userCriteria, 
        CancellationToken cancellationToken = default);
}