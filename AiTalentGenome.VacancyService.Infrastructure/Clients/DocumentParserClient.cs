using AiTalentGenome.Contracts.Parser;
using AiTalentGenome.VacancyService.Application.Interfaces;
using Google.Protobuf;

namespace AiTalentGenome.VacancyService.Infrastructure.Clients;

public class DocumentParserClient(DocumentParser.DocumentParserClient client) : IDocumentParserClient
{
    public async Task<VacancyResponse> ParseVacancyAsync(byte[] content, string extension, CancellationToken ct = default)
    {
        var request = new ParseRequest
        {
            FileContent = ByteString.CopyFrom(content),
            FileExtension = extension
        };

        return await client.ParseVacancyAsync(request, cancellationToken: ct);
    }

    public async Task<CandidateResponse> ParseResumeAsync(
        byte[] content, 
        string extension, 
        string vacancyTitle, 
        string vacancyDescription, 
        IEnumerable<string> vacancyKeySkills, 
        CancellationToken ct = default)
    {
        // ВАЖНО: Используем ParseResumeRequest вместо обычного ParseRequest
        var request = new ParseResumeRequest
        {
            FileContent = ByteString.CopyFrom(content),
            FileExtension = extension,
            VacancyTitle = vacancyTitle,
            VacancyDescription = vacancyDescription,
            VacancyKeySkills = { vacancyKeySkills } // Синтаксис repeated полей в gRPC
        };

        return await client.ParseResumeAsync(request, cancellationToken: ct);
    }
}