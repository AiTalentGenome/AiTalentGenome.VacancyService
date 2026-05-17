using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhCollectionItem(
    [property: JsonPropertyName("id")] string Id, // Например: "inbox", "consider"
    [property: JsonPropertyName("name")] string Name
);