using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External;

public record HhExperience(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);