namespace AiTalentGenome.VacancyService.Application.DTOs;

public record VacancyShortDto(
    Guid Id,
    string? HhId,
    string Title,
    string? EmployerName,
    DateTime CreatedAt
);