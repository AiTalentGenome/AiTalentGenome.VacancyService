namespace AiTalentGenome.VacancyService.Domain.Entities;

public record VacancySummary(
    Guid Id,
    string? HhId,
    string Title,
    DateTime CreatedAt,
    string? AreaName,
    bool IsActive,
    int ApplicationsCount
);