using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Enums;

namespace AiTalentGenome.VacancyService.Domain.Interfaces;

public interface IApplicationRepository
{
    Task AddAsync(Application application, CancellationToken ct = default);
    Task<Application?> GetByHhIdAsync(string hhId, CancellationToken ct = default);
    Task<Application?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Application>> GetByVacancyIdAsync(Guid vacancyId, CancellationToken ct = default);
    Task<List<Application>> GetFilteredAsync(
        Guid vacancyId, 
        List<ApplicationStatus>? statuses, 
        bool? onlyAnalyzed, 
        CancellationToken ct = default);
    
    Task<(List<Domain.Entities.Application> Items, int TotalCount)> GetPagedFilteredAsync(
        Guid vacancyId, 
        int page, 
        int pageSize, 
        List<ApplicationStatus>? statuses, 
        bool? onlyAnalyzed, 
        CancellationToken ct);
    
    void Update(Application application);
}