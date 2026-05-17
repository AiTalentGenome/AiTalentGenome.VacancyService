namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface ICandidateAnalysisService
{
    Task EnrichCandidateDataAsync(Guid applicationId, string accessToken);
}