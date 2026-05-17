namespace AiTalentGenome.VacancyService.Application.DTOs;

public record VacancyDetailDto(
    Guid Id,
    string Title,
    string Description,
    List<string> KeySkills,
    SalaryDto? Salary,
    string? Experience,
    string? AreaName,
    string? HhId
);