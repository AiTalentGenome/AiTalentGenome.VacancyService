using AiTalentGenome.VacancyService.Application.DTOs.External;
using AiTalentGenome.VacancyService.Application.DTOs.External.Application;

namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface IHeadHunterService
{
    Task<List<HhVacancyDto>> GetActiveVacanciesAsync(string accessToken, CancellationToken ct = default);
    Task<HhVacancyDto?> GetVacancyDetailsAsync(string accessToken, string vacancyId, CancellationToken ct = default);
    Task<List<HhApplicationDto>> GetApplicationsByVacancyAsync(string accessToken, string hhVacancyId, CancellationToken ct = default);
    Task<List<string>> GetResumeSkillsAsync(string accessToken, string resumeId, CancellationToken ct = default);
    Task<string?> GetCoverLetterAsync(string accessToken, string negotiationId, CancellationToken ct = default);
    Task<HhResumeEnrichedResult?> GetResumeRawTextAsync(string accessToken, string resumeId, CancellationToken ct = default);
}