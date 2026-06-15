namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhResumeEnrichedResult(
    string RawText,
    string? Education,
    string? LastJobTitle,
    string? LastCompany,
    int? TotalExperienceMonths
);