namespace AiTalentGenome.VacancyService.Domain.Enums;

public enum ApplicationStatus
{
    Submitted,       // Неразобранные (inbox)
    Screening,       // Подумать (consider)
    PhoneInterview,  // Первичный контакт (phone_interview)
    Assessment,      // Тестовое задание (assessment)
    Interview,       // Собеседование (interview)
    Offered,         // Предложение о работе (offer)
    Hired,           // Выход на работу (hired)
    Rejected         // Не подходит (discard)
}