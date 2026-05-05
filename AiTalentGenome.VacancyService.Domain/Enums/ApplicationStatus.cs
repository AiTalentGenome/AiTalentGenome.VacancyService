namespace AiTalentGenome.VacancyService.Domain.Enums;

public enum ApplicationStatus
{
    Submitted,    // Новый отклик
    Screening,    // Проверка AI/Рекрутером
    Interview,    // Собеседование
    Offered,      // Предложение
    Rejected,     // Отказ
    Hired         // Нанят
}