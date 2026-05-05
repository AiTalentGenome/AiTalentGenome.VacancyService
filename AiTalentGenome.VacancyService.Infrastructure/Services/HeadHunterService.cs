using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiTalentGenome.VacancyService.Application.DTOs.External;
using AiTalentGenome.VacancyService.Application.DTOs.External.Application;
using AiTalentGenome.VacancyService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class HeadHunterService(HttpClient httpClient, ILogger<HeadHunterService> logger) : IHeadHunterService
{
    private const string BaseUrl = "https://api.hh.ru/";

    public async Task<List<HhVacancyDto>> GetActiveVacanciesAsync(string accessToken, CancellationToken ct = default)
    {
        // 1. Настраиваем заголовки правильно (обязательно с email!)
        SetAuthHeaders(accessToken);

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

    public async Task<HhVacancyDto?> GetVacancyDetailsAsync(string accessToken, string vacancyId, CancellationToken ct = default)
    {
        SetAuthHeaders(accessToken);
    
        // Важно: полный JSON вакансии в HH находится по адресу /vacancies/{id}
        var response = await httpClient.GetAsync($"{BaseUrl}vacancies/{vacancyId}", ct);
    
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<HhVacancyDto>(cancellationToken: ct);
    }

    public async Task<List<HhApplicationDto>> GetApplicationsByVacancyAsync(string accessToken, string hhVacancyId, CancellationToken ct = default)
    {
        SetAuthHeaders(accessToken);
        var allApps = new List<HhApplicationDto>();
        int page = 0;
        int totalPages = 1;

        do
        {
            var url = $"https://api.hh.ru/negotiations?vacancy_id={hhVacancyId}&page={page}&per_page=50";
            var response = await httpClient.GetFromJsonAsync<HhNegotiationsResponse>(url, ct);
        
            if (response?.Items == null) break;
            totalPages = response.Pages;

            var mapped = response.Items.Select(i => new HhApplicationDto(
                i.Id,
                i.ShortResume.Id,
                $"{i.ShortResume.FirstName} {i.ShortResume.LastName}",
                i.ShortResume.Title,
                i.ShortResume.AlternateUrl,
                i.State.Id
            ));

            allApps.AddRange(mapped);
            page++;
        } while (page < totalPages);

        return allApps;
    }
    
    private void SetAuthHeaders(string accessToken)
    {
        // Очищаем старые заголовки, чтобы не было конфликтов при повторных вызовах
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        // HH требует уникальный User-Agent с контактными данными
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AiTalentGenome/1.0 (thirty.sixth@yandex.ru)");
    }
    
    // Вспомогательные модели для десериализации /me
    private record HhMeResponse(HhEmployer? Employer);
    private record HhEmployer(string Id);
    private record HhResponseRoot(List<HhVacancyDto> Items);
}