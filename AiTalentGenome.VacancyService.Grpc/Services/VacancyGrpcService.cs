using AiTalentGenome.Contracts.Identity;
using AiTalentGenome.Contracts.Vacancies;
using AiTalentGenome.VacancyService.Application.Features.Applications.Commands;
using AiTalentGenome.VacancyService.Application.Features.Applications.Queries;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Commands;
using AiTalentGenome.VacancyService.Application.Features.Vacancies.Queries;
using AiTalentGenome.VacancyService.Domain.Enums;
using AiTalentGenome.VacancyService.Domain.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using DomainStatus = AiTalentGenome.VacancyService.Domain.Enums.ApplicationStatus;
using ContractStatus = AiTalentGenome.Contracts.Vacancies.ApplicationStatus;
using Enum = Google.Protobuf.WellKnownTypes.Enum;

namespace AiTalentGenome.VacancyService.Grpc.Services;

public class VacancyGrpcService(
    IMediator mediator,
    IdentityService.IdentityServiceClient identityClient,
    IUnitOfWork unitOfWork) : Contracts.Vacancies.VacancyService.VacancyServiceBase
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

    public override async Task<GetVacanciesResponse> GetVacancies(GetVacanciesRequest request,
        ServerCallContext context)
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
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(v.CreatedAt, DateTimeKind.Utc)),
            AreaName = v.AreaName ?? string.Empty,
            IsActive = v.IsActive,
            ApplicationsCount = v.ApplicationsCount
        }));

        return response;
    }

    public override async Task<ApplicationResponse> AddManualCandidate(AddManualCandidateRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.VacancyId, out var vacancyGuid))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Vacancy ID format"));
        }

        try
        {
            var command = new AddManualCandidateCommand(
                vacancyGuid,
                request.CandidateName,
                request.CandidateEmail,
                request.CandidatePhone,
                request.ResumeUrl,
                request.CoverLetter
            );

            var applicationId = await mediator.Send(command);

            return new ApplicationResponse
            {
                Id = applicationId.ToString(),
                Status = ContractStatus.Submitted,
                Message = "Кандидат успешно добавлен вручную"
            };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Вакансия не найдена"));
        }
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
            Salary = result.Salary != null
                ? new Salary
                {
                    From = result.Salary.From ?? 0,
                    To = result.Salary.To ?? 0,
                    Currency = result.Salary.Currency ?? string.Empty
                }
                : null,
            Experience = result.Experience ?? string.Empty,
            AreaName = result.AreaName ?? string.Empty,
            HhId = result.HhId ?? string.Empty
        };
    }

    public override async Task<SyncApplicationsResponse> SyncApplications(SyncApplicationsRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.VacancyId, out var vacancyGuid))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Некорректный ID вакансии"));
        }

        // 2. Находим вакансию в БД, чтобы получить её внешний HhId
        var vacancy = await unitOfWork.Vacancies.GetByIdAsync(vacancyGuid);
        if (vacancy == null || string.IsNullOrEmpty(vacancy.HhId))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Вакансия не найдена или не связана с HH"));
        }

        // 3. Вызываем команду синхронизации откликов
        var command = new SyncApplicationsCommand(
            vacancy.Id,
            vacancy.HhId,
            request.AccessToken
        );

        var count = await mediator.Send(command);

        return new SyncApplicationsResponse
        {
            SyncedCount = count,
            Message = $"Синхронизация завершена. Добавлено/обновлено откликов: {count}"
        };
    }

    public override async Task<VacancyResponse> CreateVacancyFromFile(UploadFileRequest request,
        ServerCallContext context)
    {
        // 1. Получаем инфо о пользователе через Identity (как в синхронизации)
        var userInfo = await identityClient.GetUserInfoAsync(new GetUserInfoRequest
        {
            AccessToken = request.AccessToken
        });

        if (!userInfo.IsActive)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Пользователь не активен"));
        }

        // 2. Отправляем команду в Application слой
        var command = new CreateVacancyFromFileCommand(
            request.FileContent.ToByteArray(),
            request.Extension,
            userInfo.Id, // OwnerId
            userInfo.Id // CompanyId (или userInfo.CompanyId, если есть в контракте)
        );

        var vacancyId = await mediator.Send(command, context.CancellationToken);

        // 3. Получаем созданную вакансию, чтобы вернуть её данные (или вызываем GetVacancyByIdQuery)
        var result = await mediator.Send(new GetVacancyByIdQuery(vacancyId));

        return new VacancyResponse
        {
            Id = result.Id.ToString(),
            Title = result.Title,
            Description = result.Description,
            KeySkills = { result.KeySkills },
            Salary = result.Salary != null
                ? new Salary
                {
                    From = result.Salary.From ?? 0,
                    To = result.Salary.To ?? 0,
                    Currency = result.Salary.Currency ?? "KZT"
                }
                : null,
            Experience = result.Experience ?? string.Empty,
            AreaName = result.AreaName ?? string.Empty,
            HhId = result.HhId ?? string.Empty
        };
    }

    public override async Task<ApplicationResponse> AddCandidateFromFile(UploadCandidateFileRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.VacancyId, out var vacancyGuid))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Некорректный формат GUID вакансии"));
        }

        // Здесь токен не обязателен, если мы просто добавляем кандидата к вакансии, 
        // но можно добавить проверку прав доступа к вакансии.

        var command = new AddCandidateFromResumeCommand(
            vacancyGuid,
            request.FileContent.ToByteArray(),
            request.Extension
        );

        var applicationId = await mediator.Send(command, context.CancellationToken);

        return new ApplicationResponse
        {
            Id = applicationId.ToString(),
            Status = ContractStatus.Submitted,
            Message = "Кандидат успешно извлечен из файла и добавлен"
        };
    }
    
    public override async Task<GetApplicationsResponse> GetApplicationsByVacancy(
        GetApplicationsRequest request, 
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.VacancyId, out var vacancyId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ID"));
        }

        // 1. ЕслиStatuses в proto — это repeated ApplicationStatus, 
        // то s — это уже числовой тип enum в C#
        var statusFilters = request.Statuses
            .Select(s => (DomainStatus)(int)s) 
            .ToList();

        var query = new GetApplicationsByVacancyQuery(vacancyId, statusFilters, request.OnlyAnalyzed);
        var result = await mediator.Send(query);

        var response = new GetApplicationsResponse();
        response.Applications.AddRange(result.Select(a => new ApplicationDetail
        {
            Id = a.Id.ToString(),
            CandidateName = a.CandidateName,
            CandidateEmail = a.CandidateEmail,
            LastJobTitle = a.LastJobTitle ?? "Не указано",
            TotalExperienceMonths = a.TotalExperienceMonths ?? 0,
            AiScore = a.AiScore ?? 0,
            
            // ИСПРАВЛЕНИЕ: Прямое приведение Domain Enum -> Contract Enum через int
            Status = (ContractStatus)(int)a.Status, 
            
            CandidateSkills = { a.CandidateSkills },
            AppliedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(a.AppliedAt, DateTimeKind.Utc))
        }));

        return response;
    }
    
    public override async Task<GetPagedApplicationsResponse> GetPagedApplicationsByVacancy(
        GetPagedApplicationsRequest request, 
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.VacancyId, out var vacancyId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Некорректный формат GUID вакансии"));
        }

        // Маппинг контрактов enum в доменные enum
        var statusFilters = request.Statuses
            .Select(s => (DomainStatus)(int)s) 
            .ToList();

        // Вызываем новый Query
        var query = new GetPagedApplicationsByVacancyQuery(
            vacancyId,
            request.Page,
            request.PageSize,
            statusFilters,
            request.OnlyAnalyzed
        );

        var result = await mediator.Send(query, context.CancellationToken);

        var response = new GetPagedApplicationsResponse
        {
            TotalCount = result.TotalCount
        };

        response.Applications.AddRange(result.Items.Select(a => new ApplicationDetail
        {
            Id = a.Id.ToString(),
            CandidateName = a.CandidateName,
            CandidateEmail = a.CandidateEmail,
            LastJobTitle = a.LastJobTitle,
            TotalExperienceMonths = a.TotalExperienceMonths ?? 0,
            AiScore = a.AiScore ?? 0.0,
            Status = (ContractStatus)(int)a.Status, // Прямой маппинг
            CandidateSkills = { a.CandidateSkills },
            AppliedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(a.AppliedAt, DateTimeKind.Utc)),
            AiAnalysisJson = a.AiAnalysisJson ?? string.Empty
        }));

        return response;
    }
    
    public override async Task<StartAiAnalysisResponse> StartAiAnalysis(
        StartAiAnalysisRequest request, 
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.VacancyId, out var vacancyGuid))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Vacancy ID format"));
        }

        var appGuids = request.ApplicationIds
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        // Передаем request.AccessToken в конструктор команды
        var command = new StartAiAnalysisCommand(vacancyGuid, appGuids, request.UserCriteria, request.AccessToken);
        var analyzedDtoList = await mediator.Send(command);

        var response = new StartAiAnalysisResponse();
    
        response.Results.AddRange(analyzedDtoList.Select(r => new AnalyzedApplicationResult
        {
            ApplicationId = r.ApplicationId.ToString(),
            AiScore = r.AiScore,
            AiAnalysisJson = r.AiAnalysisJson,
            CandidateSkills = { r.CandidateSkills }
        }));

        return response;
    }
}