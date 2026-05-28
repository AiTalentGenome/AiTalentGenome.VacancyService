namespace AiTalentGenome.VacancyService.Application.Interfaces;

public interface IRabbitMqPublisher
{
    /// <summary
    ///Публикует сообщение в RabbitMQ Exchange
    /// </summary>
    Task PublishAsync(string routingKey, string messageJson, CancellationToken ct =  default);
}