using System.Text;
using AiTalentGenome.VacancyService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AiTalentGenome.VacancyService.Infrastructure.Services;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly string _exchangeName;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ConnectionFactory _connectionFactory;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;

        var hostName = configuration["RabbitMQ:Host"] ?? "localhost";
        var userName = configuration["RabbitMQ:UserName"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";
        _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "AiTalentGenome.MainExchange";

        _connectionFactory = new ConnectionFactory()
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            AutomaticRecoveryEnabled = true
        };
    }

    private async Task EnsureChannelInitializedAsync(CancellationToken ct)
    {
        if (_channel is { IsClosed: false }) return;
        await _semaphore.WaitAsync(ct);

        try
        {
            if (_channel is { IsClosed: false }) return;

            _logger.LogInformation("Инициализация асинхронного подключения к RabbitMQ v7+...");

            _connection = await _connectionFactory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct
            );

            _logger.LogInformation("RabbitMQ успешно инициализирован с использованием IChannel. Exchange: {Exchange}",
                _exchangeName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось подключиться к RabbitMQ брокеру.");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task PublishAsync(string routingKey, string messageJson, CancellationToken ct = default)
    {
        await EnsureChannelInitializedAsync(ct);

        try
        {
            var body = Encoding.UTF8.GetBytes(messageJson);

            var properties = new BasicProperties()
            {
                Persistent = true,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            };
            
            _logger.LogDebug("Отправка сообщения в RabbitMQ. Ключ: {RoutingKey}", routingKey);

            await _channel!.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при публикации сообщения в RabbitMQ с ключом {RoutingKey}", routingKey);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Dispose();
            _connection.Dispose();
            _semaphore.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при закрытии соединений RabbitMQ.");
        }
    }
}