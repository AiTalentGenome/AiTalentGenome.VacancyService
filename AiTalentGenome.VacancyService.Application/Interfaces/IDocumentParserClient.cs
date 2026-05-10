using AiTalentGenome.Contracts.Parser;

namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface IDocumentParserClient
{
    Task<VacancyResponse> ParseVacancyAsync(byte[] content, string extension, CancellationToken ct = default);
    Task<CandidateResponse> ParseResumeAsync(
        byte[] content, 
        string extension, 
        string vacancyTitle, 
        string vacancyDescription, 
        IEnumerable<string> vacancyKeySkills, 
        CancellationToken ct = default);
}