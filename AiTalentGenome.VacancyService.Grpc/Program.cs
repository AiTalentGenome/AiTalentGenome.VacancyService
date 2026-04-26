using AiTalentGenome.Contracts.Identity;
using AiTalentGenome.VacancyService.Application.DependencyInjection;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Grpc.Services;
using AiTalentGenome.VacancyService.Infrastructure.DependencyInjection;
using AiTalentGenome.VacancyService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddGrpcClient<IdentityService.IdentityServiceClient>(options =>
{
    // Убедись, что в appsettings.json Вакансий есть этот URL
    var identityUrl = builder.Configuration["Services:IdentityUrl"] 
                      ?? throw new InvalidOperationException("IdentityUrl is not configured in VacancyService");

    options.Address = new Uri(identityUrl);
});

builder.Services.AddHttpClient<IHeadHunterService, HeadHunterService>();

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

var app = builder.Build();

app.MapGrpcService<VacancyGrpcService>();

app.Run();