using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql; // 1. ДОБАВЬ ЭТОТ USING (пакет Npgsql ставится автоматически вместе с EF Core для Postgres)
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace AiTalentGenome.VacancyService.IntegrationTests;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private static readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ai_talent_genome_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    protected VacancyDbContext DbContext { get; private set; } = null!;
    private Respawner _respawner = null!;
    private NpgsqlConnection _dbConnection = null!; // 2. МЕНЯЕМ ТИП: Вместо string храним само соединение

    public async Task InitializeAsync()
    {
        await DbContainer.StartAsync();

        var connectionString = DbContainer.GetConnectionString();

        var options = new DbContextOptionsBuilder<VacancyDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        DbContext = new VacancyDbContext(options);
        await DbContext.Database.MigrateAsync();

        // 3. ИСПРАВЛЕНИЕ: Создаем и открываем физическое соединение NpgsqlConnection
        _dbConnection = new NpgsqlConnection(connectionString);
        await _dbConnection.OpenAsync(); // Для Respawn соединение обязательно должно быть открыто

        // 4. ИСПРАВЛЕНИЕ: Передаем объект соединения вместо строки
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public async Task ResetDatabaseAsync()
    {
        // 5. ИСПРАВЛЕНИЕ: Передаем объект соединения в метод сброса таблиц
        await _respawner.ResetAsync(_dbConnection);
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        
        // Освобождаем ресурсы подключения
        if (_dbConnection != null)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        await DbContainer.StopAsync();
    }
}