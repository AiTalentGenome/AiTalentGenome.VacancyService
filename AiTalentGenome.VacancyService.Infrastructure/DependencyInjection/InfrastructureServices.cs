using AiTalentGenome.VacancyService.Domain.Interfaces;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using AiTalentGenome.VacancyService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiTalentGenome.VacancyService.Infrastructure.DependencyInjection;

public static class InfrastructureServices
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<VacancyDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions => 
            {
                npgsqlOptions.MigrationsAssembly("AiTalentGenome.VacancyService.Infrastructure");
            }));
        
        services.AddScoped<IVacancyRepository, VacancyRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>(); // Реализуется аналогично VacancyRepository
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}