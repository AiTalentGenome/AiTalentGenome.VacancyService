using AiTalentGenome.Contracts.Parser;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using AiTalentGenome.VacancyService.Infrastructure.Clients;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using AiTalentGenome.VacancyService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

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
        
        services.AddGrpcClient<DocumentParser.DocumentParserClient>(o =>
            {
                o.Address = new Uri(configuration["Services:ParserUrl"]!); 
            })
            .AddPolicyHandler(GetRetryPolicy());

        services.AddScoped<IDocumentParserClient, DocumentParserClient>();
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}

