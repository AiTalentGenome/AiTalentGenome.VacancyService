using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhCollectionsResponse(
    [property: JsonPropertyName("found")] int Found,
    // Изменяем "items" на "employer_states"
    [property: JsonPropertyName("employer_states")] List<HhCollectionItem> Collections
);