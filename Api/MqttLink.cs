using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace Omniroute.Api;

/// <summary>
/// MQTT клієнт для підключення до EcoFlow брокера
/// Після першого успішного підключення сам перепідключається та повторно підписується на топіки
/// </summary>
public class MqttLink : IDisposable
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly HashSet<string> _topics = new();
    private readonly Action<string, byte[]> _onMessage;
    private readonly Action<bool> _onConnected;
    private readonly Action<string> _onFatal;
    private readonly CancellationTokenSource _cts = new();
    private int _disposed;

    public bool IsConnected => _client.IsConnected;

    public MqttLink(
        MqttCredentials credentials,
        string userId,
        Action<string, byte[]> onMessage,
        Action<bool> onConnected,
        Action<string> onFatal)
    {
        _onMessage = onMessage;
        _onConnected = onConnected;
        _onFatal = onFatal;

        // Брокер приймає лише ClientId у форматі офіційного застосунку
        var clientId = $"ANDROID_{Guid.NewGuid():N}".ToUpperInvariant() + $"_{userId}";

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(credentials.Host, credentials.Port)
            .WithCredentials(credentials.Username, credentials.Password)
            .WithClientId(clientId)
            .WithTlsOptions(o => o.UseTls())
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithTimeout(TimeSpan.FromSeconds(20))
            .Build();

        _client = new MqttFactory().CreateMqttClient();

        // Обробники подій
        _client.ConnectedAsync += OnConnectedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
    }

    /// <summary>
    /// Підключитися до MQTT брокера. Помилка першого підключення викидається назовні.
    /// </summary>
    public async Task ConnectAsync(IEnumerable<string> initialTopics)
    {
        lock (_topics)
        {
            _topics.UnionWith(initialTopics);
        }

        await _client.ConnectAsync(_options, _cts.Token);
        _ = Task.Run(() => ReconnectLoopAsync(_cts.Token));
    }

    /// <summary>
    /// Підписатися на нові топіки
    /// </summary>
    public async Task SubscribeAsync(IEnumerable<string> newTopics)
    {
        List<string> added;
        lock (_topics)
        {
            added = newTopics.Where(_topics.Add).ToList();
        }

        if (added.Count > 0 && _client.IsConnected)
        {
            await SubscribeTopicsAsync(added);
        }
    }

    /// <summary>
    /// Відписатися від топіків
    /// </summary>
    public async Task UnsubscribeAsync(IEnumerable<string> oldTopics)
    {
        List<string> removed;
        lock (_topics)
        {
            removed = oldTopics.Where(_topics.Remove).ToList();
        }

        if (removed.Count > 0 && _client.IsConnected)
        {
            var builder = new MqttClientUnsubscribeOptionsBuilder();
            foreach (var topic in removed)
            {
                builder.WithTopicFilter(topic);
            }
            await _client.UnsubscribeAsync(builder.Build(), _cts.Token);
        }
    }

    /// <summary>
    /// Опублікувати повідомлення в топік
    /// </summary>
    public async Task<bool> PublishAsync(string topic, byte[] payload)
    {
        if (!_client.IsConnected)
            return false;

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(message, _cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MQTT publish failed: {ex.Message}");
            return false;
        }
    }

    private async Task SubscribeTopicsAsync(IReadOnlyCollection<string> topics)
    {
        // Кожен топік — окремий фільтр
        var builder = new MqttClientSubscribeOptionsBuilder();
        foreach (var topic in topics)
        {
            builder.WithTopicFilter(f => f
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce));
        }
        await _client.SubscribeAsync(builder.Build(), _cts.Token);
    }

    /// <summary>
    /// Перевіряє з'єднання й перепідключається, доки клієнт не закрито
    /// </summary>
    private async Task ReconnectLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ReconnectDelay, token);
                if (_client.IsConnected)
                    continue;

                await _client.ConnectAsync(_options, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (MqttConnectingFailedException ex) when (IsAuthFailure(ex.ResultCode))
            {
                // Облікові дані MQTT більше не дійсні: повторні спроби не допоможуть
                _onFatal("MQTT: доступ заборонено");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT reconnect failed: {ex.Message}");
            }
        }
    }

    public static bool IsAuthFailure(MqttClientConnectResultCode code) =>
        code is MqttClientConnectResultCode.BadUserNameOrPassword or MqttClientConnectResultCode.NotAuthorized;

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MQTT connected");
        _onConnected(true);

        // Повторно підписатися на всі топіки (сесія чиста після кожного підключення)
        List<string> allTopics;
        lock (_topics)
        {
            allTopics = _topics.ToList();
        }

        if (allTopics.Count > 0)
        {
            try
            {
                await SubscribeTopicsAsync(allTopics);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT resubscribe failed: {ex.Message}");
            }
        }
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        if (e.ClientWasConnected)
        {
            System.Diagnostics.Debug.WriteLine($"MQTT disconnected: {e.Reason}");
            _onConnected(false);
        }
        return Task.CompletedTask;
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            _onMessage(e.ApplicationMessage.Topic, e.ApplicationMessage.PayloadSegment.ToArray());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Message handling failed on {e.ApplicationMessage.Topic}: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _cts.Cancel();
        _client.ConnectedAsync -= OnConnectedAsync;
        _client.DisconnectedAsync -= OnDisconnectedAsync;
        _client.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync;

        // Коректно відключитися, але не чекати довше 3 с
        try
        {
            if (_client.IsConnected)
            {
                _client.DisconnectAsync().Wait(TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MQTT disconnect failed: {ex.Message}");
        }

        _client.Dispose();
    }
}
