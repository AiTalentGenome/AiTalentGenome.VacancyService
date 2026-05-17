using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhNegotiationItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("state")] HhState State,
    [property: JsonPropertyName("resume")] HhResume ShortResume,
    [property: JsonPropertyName("messages_url")] string MessagesUrl,
    [property: JsonPropertyName("cover_letter")] string? CoverLetter
);