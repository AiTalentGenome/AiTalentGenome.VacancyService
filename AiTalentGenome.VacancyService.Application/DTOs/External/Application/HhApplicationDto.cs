namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhApplicationDto(
    string NegotiationId,
    string ResumeId,
    string FullName,
    string? Position,
    string? ResumeUrl,
    string? StateId,
    string? Email,  // Добавлено
    string? Phone,  // Добавлено,
    string? CoverLetter,
    List<string> Skills
);