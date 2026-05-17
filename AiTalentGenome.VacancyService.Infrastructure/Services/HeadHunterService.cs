using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AiTalentGenome.VacancyService.Application.DTOs.External;
using AiTalentGenome.VacancyService.Application.DTOs.External.Application;
using AiTalentGenome.VacancyService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class HeadHunterService(HttpClient httpClient, ILogger<HeadHunterService> logger) : IHeadHunterService
{
    private const string BaseUrl = "https://api.hh.ru/";
    
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true 
    };

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

    // В файле HeadHunterService.cs
    public async Task<List<HhApplicationDto>> GetApplicationsByVacancyAsync(string accessToken, string hhVacancyId, CancellationToken ct = default)
    {
        SetAuthHeaders(accessToken);
        var allApps = new List<HhApplicationDto>();

        var collectionsUrl = $"{BaseUrl}negotiations?vacancy_id={hhVacancyId}";
        var collectionsResponse = await httpClient.GetFromJsonAsync<HhCollectionsResponse>(collectionsUrl, JsonOptions, ct);

        if (collectionsResponse?.Collections == null) return allApps;

        foreach (var collection in collectionsResponse.Collections)
        {
            int page = 0;
            int totalPages = 1;
            do
            {
                var url = $"{BaseUrl}negotiations/{collection.Id}?vacancy_id={hhVacancyId}&page={page}&per_page=50";
                var response = await httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) break;

                var data = await response.Content.ReadFromJsonAsync<HhNegotiationsResponse>(JsonOptions, ct);

                if (data?.Items != null)
                {
                    var mapped = data.Items.Select(i => new HhApplicationDto(
                        i.Id,
                        i.ShortResume?.Id ?? "no_id",
                        $"{i.ShortResume?.FirstName} {i.ShortResume?.LastName}".Trim(),
                        i.ShortResume?.Title,
                        i.ShortResume?.AlternateUrl,
                        collection.Id,
                        null, // Email пока пустой
                        null, // Phone пока пустой
                        null, // CoverLetter пока пустой
                        new List<string>() // Skills пока пустые
                    ));
                    allApps.AddRange(mapped);
                    totalPages = data.Pages;
                }
                page++;
            } while (page < totalPages);
        }
        return allApps;
    }

    public async Task<List<string>> GetResumeSkillsAsync(string accessToken, string resumeId, CancellationToken ct = default)
    {
        SetAuthHeaders(accessToken);
    
        try 
        {
            // Эндпоинт для получения полного текста резюме
            var url = $"{BaseUrl}resumes/{resumeId}";
            var response = await httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode) return new List<string>();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
        
            // Извлекаем skill_set как список строк
            if (doc.RootElement.TryGetProperty("skill_set", out var skillElement))
            {
                return skillElement.EnumerateArray()
                    .Select(s => s.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении навыков для резюме {Id}", resumeId);
        }

        return new List<string>();
    }
    
    private void SetAuthHeaders(string accessToken)
    {
        // Очищаем старые заголовки, чтобы не было конфликтов при повторных вызовах
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        // HH требует уникальный User-Agent с контактными данными
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AiTalentGenome/1.0 (thirty.sixth@yandex.ru)");
    }
    
    // Измени сигнатуру и сделай метод публичным (и добавь в интерфейс IHeadHunterService)
    public async Task<string?> GetCoverLetterAsync(string accessToken, string negotiationId, CancellationToken ct = default)
    {
        SetAuthHeaders(accessToken);

        try 
        {
            // Формируем URL напрямую по ID отклика
            var url = $"{BaseUrl}negotiations/{negotiationId}/messages";
            var response = await httpClient.GetFromJsonAsync<HhMessagesResponse>(url, JsonOptions, ct);
    
            // Ищем сообщение от кандидата (applicant)
            return response?.Items?
                .FirstOrDefault(m => m.Author?.ParticipantType == "applicant")?.Text;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке письма для отклика {Id}", negotiationId);
            return null;
        }
    }
    
    // Вспомогательные модели для десериализации /me
    private record HhMeResponse(HhEmployer? Employer);
    private record HhEmployer(string Id);
    private record HhResponseRoot(List<HhVacancyDto> Items);
}