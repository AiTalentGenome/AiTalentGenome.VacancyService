using DomainStatus = AiTalentGenome.VacancyService.Domain.Enums.ApplicationStatus;

namespace AiTalentGenome.VacancyService.Application.DTOs;

public record ApplicationResponseDto(
    Guid Id,
    string CandidateName,
    string CandidateEmail,
    string? LastJobTitle,
    int? TotalExperienceMonths,
    double? AiScore,
    DomainStatus Status,
    List<string> CandidateSkills,
    DateTime AppliedAt
);