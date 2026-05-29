using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using MediatR;

namespace AiTalentGenome.VacancyService.Application.Features.Vacancies.Handlers;

public class StartAiAnalysisHandler : IRequestHandler<StartAiAnalysisCommand, List<AnalyzedCandidateResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICandidateAnalysisService _analysisService;
    private readonly IHeadHunterService _hhService; // Добавляем сервис интеграции с HH

    public StartAiAnalysisHandler(
        IUnitOfWork unitOfWork, 
        ICandidateAnalysisService analysisService, 
        IHeadHunterService hhService)
    {
        _unitOfWork = unitOfWork;
        _analysisService = analysisService;
        _hhService = hhService;
    }

    public async Task<List<AnalyzedCandidateResultDto>> Handle(StartAiAnalysisCommand request, CancellationToken cancellationToken)
{
    var vacancy = await _unitOfWork.Vacancies.GetByIdAsync(request.VacancyId);
    if (vacancy == null) throw new KeyNotFoundException("Vacancy not found");

    var results = new List<AnalyzedCandidateResultDto>();

    foreach (var appId in request.ApplicationIds)
    {
        var application = await _unitOfWork.Applications.GetByIdAsync(appId);
        if (application == null || application.VacancyId != vacancy.Id) continue;

        // Если текста резюме нет в БД, скачиваем его из HH на лету, используя НАСТОЯЩИЙ токен!
        if (string.IsNullOrWhiteSpace(application.RawResumeText) && !string.IsNullOrEmpty(application.HhResumeId))
        {
            Console.WriteLine($"[AI Pre-Processor] Resume text is empty for candidate {application.CandidateName}. Fetching from HH API...");
            
            // Прокидываем request.AccessToken, полученный из шлюза
            var fetchedText = await _hhService.GetResumeRawTextAsync(request.AccessToken, application.HhResumeId, cancellationToken);
            
            if (!string.IsNullOrEmpty(fetchedText))
            {
                application.RawResumeText = fetchedText;
                
                if (string.IsNullOrEmpty(application.CoverLetter) && !string.IsNullOrEmpty(application.HhNegotiationId))
                {
                    application.CoverLetter = await _hhService.GetCoverLetterAsync(request.AccessToken, application.HhNegotiationId, cancellationToken);
                }
                
                await _unitOfWork.SaveChangesAsync();
            }
        }

        if (string.IsNullOrWhiteSpace(application.RawResumeText))
        {
            Console.WriteLine($"[AI Pre-Processor Warning] Skipping candidate {application.CandidateName}: No resume content available.");
            continue;
        }

        // Вызов Python-микросервиса
        var (aiScore, aiAnalysisJson, extractedSkills) = await _analysisService.AnalyzeApplicationAsync(
            vacancy, 
            application, 
            request.UserCriteria, 
            cancellationToken
        );

        application.AiScore = aiScore;
        application.AiAnalysisJson = aiAnalysisJson;
        application.CandidateSkills = extractedSkills;

        results.Add(new AnalyzedCandidateResultDto(
            application.Id,
            aiScore,
            aiAnalysisJson,
            extractedSkills
        ));
    }

    await _unitOfWork.SaveChangesAsync();
    return results;
}
}