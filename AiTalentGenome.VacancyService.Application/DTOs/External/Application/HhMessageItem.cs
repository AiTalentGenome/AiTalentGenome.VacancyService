using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhMessageItem(
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("author")] HhAuthor Author
);