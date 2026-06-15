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

    public async Task<HhVacancyDto?> GetVacancyDetailsAsync(string accessToken, string vacancyId,
        CancellationToken ct = default)
    {
        SetAuthHeaders(accessToken);

        // Важно: полный JSON вакансии в HH находится по адресу /vacancies/{id}
        var response = await httpClient.GetAsync($"{BaseUrl}vacancies/{vacancyId}", ct);

        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<HhVacancyDto>(cancellationToken: ct);
    }

    // В файле HeadHunterService.cs
    public async Task<List<HhApplicationDto>> GetApplicationsByVacancyAsync(string accessToken, string hhVacancyId,
        CancellationToken ct = default)
    {
        SetAuthHeaders(accessToken);
        var allApps = new List<HhApplicationDto>();

        var collectionsUrl = $"{BaseUrl}negotiations?vacancy_id={hhVacancyId}";
        var collectionsResponse =
            await httpClient.GetFromJsonAsync<HhCollectionsResponse>(collectionsUrl, JsonOptions, ct);

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

    public async Task<List<string>> GetResumeSkillsAsync(string accessToken, string resumeId,
        CancellationToken ct = default)
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

    public async Task<HhResumeEnrichedResult?> GetResumeRawTextAsync(string accessToken, string resumeId,
        CancellationToken ct = default)
    {
        SetAuthHeaders(accessToken);

        try
        {
            var url = $"{BaseUrl}resumes/{resumeId}";
            var response = await httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Не удалось получить резюме {ResumeId} из HH. Статус: {Status}", resumeId,
                    response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var sb = new System.Text.StringBuilder();

            string? lastJobTitle = null;
            string? lastCompany = null;
            int? totalExperienceMonths = null;
            string? educationLevel = null;

            // 1. Основная информация
            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                sb.AppendLine($"Желаемая должность: {title.GetString()}");
            }

            // 2. Желаемая зарплата
            if (root.TryGetProperty("salary", out var salary) && salary.ValueKind == JsonValueKind.Object)
            {
                var amount = salary.TryGetProperty("amount", out var am) && am.ValueKind == JsonValueKind.Number
                    ? am.GetRawText()
                    : "";
                var currency = salary.TryGetProperty("currency", out var cur) && cur.ValueKind == JsonValueKind.String
                    ? cur.GetString()
                    : "";
                if (!string.IsNullOrEmpty(amount))
                {
                    sb.AppendLine($"Желаемая зарплата: {amount} {currency}");
                }
            }

            // 3. Опыт работы (Извлекаем метаданные)
            if (root.TryGetProperty("experience", out var experienceList) &&
                experienceList.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine("\n=== ОПЫТ РАБОТЫ ===");
                var experiences = experienceList.EnumerateArray().ToList();

                // HH возвращает опыт в хронологическом порядке (сверху самое последнее место работы)
                if (experiences.Count > 0 && experiences[0].ValueKind == JsonValueKind.Object)
                {
                    var latestExp = experiences[0];
                    lastJobTitle =
                        latestExp.TryGetProperty("position", out var pos) && pos.ValueKind == JsonValueKind.String
                            ? pos.GetString()
                            : null;
                    lastCompany =
                        latestExp.TryGetProperty("company", out var comp) && comp.ValueKind == JsonValueKind.Object &&
                        comp.TryGetProperty("name", out var cn)
                            ? cn.GetString()
                            : null;
                }

                foreach (var exp in experiences)
                {
                    if (exp.ValueKind != JsonValueKind.Object) continue;

                    var companyName =
                        exp.TryGetProperty("company", out var comp) && comp.ValueKind == JsonValueKind.Object &&
                        comp.TryGetProperty("name", out var cn)
                            ? cn.GetString()
                            : "Не указана";
                    var position = exp.TryGetProperty("position", out var pos) && pos.ValueKind == JsonValueKind.String
                        ? pos.GetString()
                        : "Не указана";
                    var start = exp.TryGetProperty("start", out var st) && st.ValueKind == JsonValueKind.String
                        ? st.GetString()
                        : "";
                    var end = exp.TryGetProperty("end", out var nd) && nd.ValueKind == JsonValueKind.String
                        ? nd.GetString()
                        : "По настоящее время";
                    var description =
                        exp.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String
                            ? desc.GetString()
                            : "";

                    sb.AppendLine($"Период: {start} — {end}");
                    sb.AppendLine($"Компания: {companyName}");
                    sb.AppendLine($"Должность: {position}");
                    if (!string.IsNullOrEmpty(description))
                    {
                        sb.AppendLine($"Обязанности и достижения:\n{description}");
                    }

                    sb.AppendLine(new string('-', 30));
                }
            }

            // Подсчет общего стажа (если HH отдает total_experience, забираем его)
            if (root.TryGetProperty("total_experience", out var totalExp) && totalExp.ValueKind == JsonValueKind.Object)
            {
                if (totalExp.TryGetProperty("months", out var m) && m.ValueKind == JsonValueKind.Number)
                {
                    totalExperienceMonths = m.GetInt32();
                }
            }

            // 4. Ключевые навыки
            if (root.TryGetProperty("skill_set", out var skills) && skills.ValueKind == JsonValueKind.Array)
            {
                var skillsList = skills.EnumerateArray()
                    .Where(s => s.ValueKind == JsonValueKind.String)
                    .Select(s => s.GetString())
                    .Where(s => !string.IsNullOrEmpty(s));

                sb.AppendLine($"\nКлючевые навыки: {string.Join(", ", skillsList)}");
            }

            // 5. Обо мне
            if (root.TryGetProperty("skills", out var aboutMe) && aboutMe.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(aboutMe.GetString()))
            {
                sb.AppendLine($"\nОбо мне:\n{aboutMe.GetString()}");
            }

            // 6. Образование
            if (root.TryGetProperty("education", out var edu) && edu.ValueKind == JsonValueKind.Object)
            {
                if (edu.TryGetProperty("level", out var lvl) && lvl.ValueKind == JsonValueKind.Object &&
                    lvl.TryGetProperty("name", out var ln))
                {
                    educationLevel = ln.GetString();
                }

                sb.AppendLine($"\n=== ОБРАЗОВАНИЕ ({(educationLevel ?? "Указано")}) ===");

                if (edu.TryGetProperty("primary", out var primaryEdu) && primaryEdu.ValueKind == JsonValueKind.Array)
                {
                    foreach (var school in primaryEdu.EnumerateArray())
                    {
                        if (school.ValueKind != JsonValueKind.Object) continue;

                        var name = school.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
                            ? n.GetString()
                            : "";
                        var organization =
                            school.TryGetProperty("organization", out var org) && org.ValueKind == JsonValueKind.String
                                ? org.GetString()
                                : "";
                        var result =
                            school.TryGetProperty("result", out var res) && res.ValueKind == JsonValueKind.String
                                ? res.GetString()
                                : "";
                        var year = school.TryGetProperty("year", out var yr) && yr.ValueKind == JsonValueKind.Number
                            ? yr.GetRawText()
                            : "";

                        sb.AppendLine(
                            $"- {year}г. {name} {(string.IsNullOrEmpty(organization) ? "" : $"({organization})")}, Специальность: {result}");
                    }
                }
            }
            else if (root.TryGetProperty("education", out var eduStr) && eduStr.ValueKind == JsonValueKind.String)
            {
                educationLevel = eduStr.GetString();
                sb.AppendLine($"\n=== ОБРАЗОВАНИЕ ===");
                sb.AppendLine($"Уровень: {educationLevel}");
            }

            return new HhResumeEnrichedResult(
                sb.ToString(),
                educationLevel,
                lastJobTitle,
                lastCompany,
                totalExperienceMonths
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при генерации RawResumeText из JSON для резюме {Id}", resumeId);
            return null;
        }
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
    public async Task<string?> GetCoverLetterAsync(string accessToken, string negotiationId,
        CancellationToken ct = default)
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