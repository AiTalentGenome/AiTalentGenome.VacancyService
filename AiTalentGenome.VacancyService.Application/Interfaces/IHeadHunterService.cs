using AiTalentGenome.VacancyService.Application.DTOs.External;

namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface IHeadHunterService
{
    Task<List<HhVacancyDto>> GetActiveVacanciesAsync(string accessToken, CancellationToken ct = default);
}