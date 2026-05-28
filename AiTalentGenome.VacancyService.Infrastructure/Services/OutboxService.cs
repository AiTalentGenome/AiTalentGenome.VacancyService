using System.Text.Json;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Entities;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class OutboxService : IOutboxService
{
    private readonly ILogger<OutboxService> _logger;
    private readonly VacancyDbContext _vacancyDbContext;
    private readonly IRabbitMqPublisher _messageBroker;
    private readonly List<OutboxMessage> _currentTxMessages = [];

    public OutboxService(VacancyDbContext vacancyDbContext, IRabbitMqPublisher messageBroker, ILogger<OutboxService> logger)
    {
        _vacancyDbContext = vacancyDbContext;
        _messageBroker = messageBroker;
        _logger = logger;
    }
    
    public void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = integrationEvent.GetType().Name,
            Content = JsonSerializer.Serialize(integrationEvent),
            OccuredOnUtc = DateTime.UtcNow
        };
        
        _vacancyDbContext.OutboxMessages.Add(outboxMessage);
        _currentTxMessages.Add(outboxMessage);
    }

    public async Task FlushOutgoingMessagesAsync(CancellationToken ct = default)
    {
        if (_currentTxMessages.Count == 0) return;
        
        _logger.LogInformation("Коммит успешен. Начинается Flush Outbox для {Count} сообщений...", _currentTxMessages.Count);

        foreach (var message in _currentTxMessages)
        {
            try
            {
                await _messageBroker.PublishAsync(message.Type, message.Content, ct);
                
                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Не удалось мгновенно отправить Outbox сообщение {Id} в брокер.", message.Id);
                message.Error = e.Message;
            }
        }
        
        await _vacancyDbContext.SaveChangesAsync(ct);
        
        _currentTxMessages.Clear();
    }
}