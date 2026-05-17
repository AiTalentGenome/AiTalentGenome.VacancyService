using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhRole(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);