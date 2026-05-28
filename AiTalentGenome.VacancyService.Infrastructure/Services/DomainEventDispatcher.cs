using AiTalentGenome.VacancyService.Application.DTOs;
using AiTalentGenome.VacancyService.Application.Interfaces;
using AiTalentGenome.VacancyService.Domain.Common;
using AiTalentGenome.VacancyService.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class DomainEventDispatcher(
    VacancyDbContext context, 
    IMediator mediator,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task<DomainDispatchResult> DispatchAsync(CancellationToken ct = default)
    {
        try
        {
            var domainEntities = context.ChangeTracker
                .Entries<AggregateRoot>()
                .Where(x => x.Entity.DomainEvents.Any())
                .ToList();

            if (!domainEntities.Any())
            {
                return new DomainDispatchResult(true);
            }

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            domainEntities.ForEach(x => x.Entity.ClearDomainEvents());

            logger.LogInformation("Обнарежено {Count} доменных событий. Начинается рассылка...", domainEvents.Count);

            foreach (var domainEvent in domainEvents)
            {
                logger.LogInformation("Рассылка доменного события {EventName}", domainEvent.GetType().Name);
                await mediator.Publish(domainEvent, ct);
            }

            return new DomainDispatchResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, " Ошибка при обработке и рассылке Domain Evetns");
            return new DomainDispatchResult(false, $"Ошибка обработки внутренних событий: {ex.Message}");
        }
    }
}