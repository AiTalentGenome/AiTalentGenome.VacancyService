namespace AiTalentGenome.VacancyService.Application.DTOs;

public record DomainDispatchResult(bool IsSuccess, string? ErrorMessage = null);