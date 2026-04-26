using Microsoft.Extensions.DependencyInjection;

namespace AiTalentGenome.VacancyService.Application.DependencyInjection;

public static class ApplicationServices
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServices).Assembly);
        });
    }
}