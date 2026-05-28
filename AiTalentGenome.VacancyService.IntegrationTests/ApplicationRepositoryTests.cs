using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Domain.Enums;
using AiTalentGenome.VacancyService.Infrastructure.Repositories;
using Xunit;

namespace AiTalentGenome.VacancyService.IntegrationTests;

public class ApplicationRepositoryTests : BaseIntegrationTest
{
    // Вместо readonly поля делаем свойство, которое каждый раз берет актуальный DbContext,
    // либо инициализируем его прямо в методах. Так надежнее всего.
    private ApplicationRepository GetRepository() => new(DbContext);

    [Fact]
    public async Task GetPagedFilteredAsync_WithStatusFilter_ShouldReturnOnlyMatchingStatuses()
    {
        // Теперь к этому моменту InitializeAsync() отработал, Docker запущен, DbContext не null!
        await ResetDatabaseAsync();

        // 1. ARRANGE
        var testVacancy = new Vacancy 
        { 
            Id = Guid.NewGuid(), 
            Title = "Тестовая вакансия", 
            Description = "Описание",
            OwnerId = 1,
            CompanyId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await DbContext.Vacancies.AddAsync(testVacancy);
        await DbContext.SaveChangesAsync();

        var appSubmitted = new Domain.Entities.Application 
        { 
            Id = Guid.NewGuid(), 
            VacancyId = testVacancy.Id,
            CandidateName = "Кандидат 1", 
            Status = ApplicationStatus.Submitted, 
            AiScore = 50, 
            AppliedAt = DateTime.UtcNow 
        };
        
        var appInterview = new Domain.Entities.Application 
        { 
            Id = Guid.NewGuid(), 
            VacancyId = testVacancy.Id,
            CandidateName = "Кандидат 2", 
            Status = ApplicationStatus.Interview, 
            AiScore = 60, 
            AppliedAt = DateTime.UtcNow 
        };

        await DbContext.Applications.AddRangeAsync(appSubmitted, appInterview);
        await DbContext.SaveChangesAsync();

        // Получаем репозиторий со свежим, готовым DbContext
        var repository = GetRepository();

        // 2. ACT
        var (items, totalCount) = await repository.GetPagedFilteredAsync(
            vacancyId: testVacancy.Id,
            page: 1,
            pageSize: 10,
            statuses: [ApplicationStatus.Interview],
            onlyAnalyzed: false,
            ct: CancellationToken.None);

        // 3. ASSERT
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("Кандидат 2", items[0].CandidateName);
    }
}