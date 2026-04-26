using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiTalentGenome.VacancyService.Application.DTOs.External;
using AiTalentGenome.VacancyService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class HeadHunterService(HttpClient httpClient, ILogger<HeadHunterService> logger) : IHeadHunterService
{
    private const string BaseUrl = "https://api.hh.ru/";

    public async Task<List<HhVacancyDto>> GetActiveVacanciesAsync(string accessToken, CancellationToken ct = default)
    {
        // 1. Настраиваем заголовки правильно (обязательно с email!)
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AiTalentGenome/1.0 (thirty.sixth@yandex.ru)");

        try 
        {
            // 2. Сначала получаем ID работодателя (как в старом проекте)
            var me = await httpClient.GetFromJsonAsync<HhMeResponse>($"{BaseUrl}me", ct);
            var employerId = me?.Employer?.Id;

            if (string.IsNullOrEmpty(employerId))
            {
                logger.LogWarning("Аккаунт HH не привязан к работодателю. Синхронизация невозможна.");
                return new List<HhVacancyDto>();
            }

            // 3. Запрашиваем вакансии конкретной компании
            // Используем per_page=100, чтобы забрать максимум за один раз
            var url = $"{BaseUrl}vacancies?employer_id={employerId}&per_page=100";
            var response = await httpClient.GetFromJsonAsync<HhResponseRoot>(url, ct);

            return response?.Items ?? new List<HhVacancyDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обращении к API HeadHunter");
            return new List<HhVacancyDto>();
        }
    }

    // Вспомогательные модели для десериализации /me
    private record HhMeResponse(HhEmployer? Employer);
    private record HhEmployer(string Id);
    private record HhResponseRoot(List<HhVacancyDto> Items);
}