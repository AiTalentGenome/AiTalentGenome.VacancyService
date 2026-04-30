using AiTalentGenome.Contracts.Identity;
using AiTalentGenome.Contracts.Vacancies;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Queries;
using Google.Protobuf.WellKnownTypes;
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

    public override async Task<GetVacanciesResponse> GetVacancies(GetVacanciesRequest request, ServerCallContext context)
    {
        var result = await mediator.Send(new GetVacanciesQuery(request.OnlyActive));

        // 2. Маппим результат в gRPC сообщение
        var response = new GetVacanciesResponse();
    
        response.Vacancies.AddRange(result.Select(v => new VacancyShort
        {
            Id = v.Id.ToString(),
            HhId = v.HhId ?? string.Empty,
            Title = v.Title,
            EmployerName = v.EmployerName ?? string.Empty,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(v.CreatedAt, DateTimeKind.Utc))
        }));

        return response;
    }

    public override async Task<VacancyResponse> GetVacancyById(GetVacancyByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var vacancyId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Некорректный формат GUID"));
        }

        var result = await mediator.Send(new GetVacancyByIdQuery(vacancyId));

        if (result == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Вакансия с ID {request.Id} не найдена"));
        }

        return new VacancyResponse
        {
            Id = result.Id.ToString(),
            Title = result.Title,
            Description = result.Description,
            KeySkills = { result.KeySkills },
            Salary = result.Salary != null ? new Salary
            {
                From = result.Salary.From ?? 0,
                To = result.Salary.To ?? 0,
                Currency = result.Salary.Currency ?? string.Empty
            } : null,
            Experience = result.Experience ?? string.Empty,
            AreaName = result.AreaName ?? string.Empty
        };
    }
}