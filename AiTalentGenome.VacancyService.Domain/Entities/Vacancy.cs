using AiTalentGenome.VacancyService.Domain.ValueObjects;

namespace AiTalentGenome.VacancyService.Domain.Entities;

public class Vacancy
{
    public Guid Id { get; set; }
    public string? HhId { get; set; } // Null, если добавлена вручную
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> KeySkills { get; set; } = new();
    public Salary? Salary { get; set; }
    public string? Experience { get; set; }
    public string? AreaName { get; set; }
    
    // Аудит и принадлежность
    public long OwnerId { get; set; } // Ссылка на AppUser.Id
    public long CompanyId { get; set; } // Ссылка на Company.Id
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Навигационное свойство
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}