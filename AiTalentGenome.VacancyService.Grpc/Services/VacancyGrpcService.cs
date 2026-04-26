using AiTalentGenome.Contracts.Identity;
using AiTalentGenome.Contracts.Vacancies;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;
using Grpc.Core;
using MediatR;

namespace AiTalentGenome.VacancyService.Grpc.Services;

public class VacancyGrpcService(
    IMediator mediator, 
    IdentityService.IdentityServiceClient identityClient) : Contracts.Vacancies.VacancyService.VacancyServiceBase
{
    public override async Task<SyncVacanciesResponse> SyncVacanciesWithHh(
        SyncVacanciesRequest request, 
        ServerCallContext context)
    {
        // 1. Сначала идем в IdentityService, чтобы получить ID пользователя и компании по токену
        // Это важно, так как в VacancyService нам нужно знать, к кому привязывать вакансии
        var userInfo = await identityClient.GetUserInfoAsync(new GetUserInfoRequest 
        { 
            AccessToken = request.AccessToken 
        });

        if (!userInfo.IsActive)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Пользователь не активен"));
        }

        // 2. Вызываем команду синхронизации в Application слое
        var command = new SyncVacanciesCommand(
            request.AccessToken, 
            userInfo.Id, 
            userInfo.Id // В твоей схеме CompanyId и UserId могут отличаться, подставь нужное
        );

        var count = await mediator.Send(command);

        return new SyncVacanciesResponse
        {
            SyncedCount = count,
            Message = $"Успешно синхронизировано {count} вакансий"
        };
    }
}