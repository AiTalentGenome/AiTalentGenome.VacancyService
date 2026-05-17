using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhAuthor(
    [property: JsonPropertyName("participant_type")] string? ParticipantType
);