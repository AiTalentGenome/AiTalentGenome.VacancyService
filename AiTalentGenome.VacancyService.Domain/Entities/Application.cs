using AiTalentGenome.VacancyService.Domain.Enums;

namespace AiTalentGenome.VacancyService.Domain.Entities;

public class Application
{
    public Guid Id { get; set; }
    
    // Связь с вакансией
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;

    // Связь с внешним миром (HH)
    public string? HhNegotiationId { get; set; } 

    // Данные кандидата
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CandidatePhone { get; set; }
    public string? ResumeUrl { get; set; } // Ссылка на PDF или страницу HH
    public string? CoverLetter { get; set; }

    // Метаданные для AI
    public double? AiScore { get; set; } // Оценка соответствия (0.0 - 1.0)
    public string? AiSummary { get; set; } // Краткое резюме от AI

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}