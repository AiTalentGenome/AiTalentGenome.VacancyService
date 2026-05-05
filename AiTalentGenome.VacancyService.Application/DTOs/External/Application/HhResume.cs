using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhResume(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("alternate_url")] string AlternateUrl
);