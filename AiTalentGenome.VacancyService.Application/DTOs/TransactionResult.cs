namespace AiTalentGenome.VacancyService.Application.DTOs;

public record TransactionResult(bool IsSuccess, string? ErrorMessage = null);