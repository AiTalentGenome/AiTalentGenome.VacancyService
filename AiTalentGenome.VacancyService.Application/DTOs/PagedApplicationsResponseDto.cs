namespace AiTalentGenome.VacancyService.Application.DTOs;

public record PagedApplicationsResponseDto(List<ApplicationResponseDto> Items, int TotalCount);