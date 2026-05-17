using System.Text.Json.Serialization;

namespace AiTalentGenome.VacancyService.Application.DTOs.External.Application;

public record HhMessagesResponse(
    [property: JsonPropertyName("items")] List<HhMessageItem>? Items
);

// Одиночное сообщение в переписке

// Автор сообщения (нам важна роль 'candidate')