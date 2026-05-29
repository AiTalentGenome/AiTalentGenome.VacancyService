namespace AiTalentGenome.VacancyService.Application.DTOs;

public record AnalyzedCandidateResultDto(
    Guid ApplicationId,
    double AiScore,
    string AiAnalysisJson,
    List<string> CandidateSkills
);