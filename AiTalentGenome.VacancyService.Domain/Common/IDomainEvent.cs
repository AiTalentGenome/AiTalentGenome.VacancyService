using MediatR;

namespace AiTalentGenome.VacancyService.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}