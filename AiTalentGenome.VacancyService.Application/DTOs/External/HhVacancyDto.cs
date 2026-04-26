using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External;

public record HhVacancyDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("salary")] HhSalary? Salary,
    [property: JsonPropertyName("area")] HhArea? Area,
    [property: JsonPropertyName("key_skills")] List<HhSkill>? KeySkills,
    // ИСПРАВЛЕНИЕ: теперь это объект, а не строка
    [property: JsonPropertyName("experience")] HhExperience? Experience 
);