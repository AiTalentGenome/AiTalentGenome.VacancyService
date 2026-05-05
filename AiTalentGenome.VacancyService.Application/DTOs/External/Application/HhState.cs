using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhState([property: JsonPropertyName("id")] string Id);