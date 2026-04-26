using AiTalentGenome.VacancyService.Domain.Enums;

namespace AiTalentGenome.VacancyService.Domain.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;

    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? ResumeUrl { get; set; }
    public string? CoverLetter { get; set; }
    
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}