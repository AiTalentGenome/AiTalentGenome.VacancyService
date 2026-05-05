using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhNegotiationsResponse(
    [property: JsonPropertyName("items")] List<HhNegotiationItem> Items,
    [property: JsonPropertyName("found")] int Found,
    [property: JsonPropertyName("pages")] int Pages,
    [property: JsonPropertyName("page")] int Page
);