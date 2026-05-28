namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface IOutboxService
{
    void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class;
    Task FlushOutgoingMessagesAsync(CancellationToken ct =  default);
}