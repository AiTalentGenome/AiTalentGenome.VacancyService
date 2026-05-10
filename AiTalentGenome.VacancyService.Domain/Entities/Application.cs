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
    
    // Полный текст резюме (без него AI не сможет провести глубокий анализ)
    public string? RawResumeText { get; set; } 
    
    // Список навыков кандидата (чтобы сравнивать с Vacancy.KeySkills на уровне БД)
    public List<string> CandidateSkills { get; set; } = new();

    // Общий стаж работы в месяцах (удобно для фильтрации: "опыт > 36 месяцев")
    public int? TotalExperienceMonths { get; set; }

    // Последняя должность и компания (для быстрого отображения в списке)
    public string? LastJobTitle { get; set; }
    public string? LastCompany { get; set; }

    // Образование (например: "Высшее, МГТУ им. Баумана")
    public string? Education { get; set; }

    // --- РАСШИРЕННЫЕ МЕТАДАННЫЕ AI ---

    // Вместо простого Summary — структурированный фидбек
    // Можно хранить как JSON: { "pros": [...], "cons": [...], "culture_fit": 0.8 }
    public string? AiAnalysisJson { get; set; } 

    // Критические несовпадения (например, "Нет знания английского", если это было в вакансии)
    public List<string> CriticalMismatches { get; set; } = new();
    
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}