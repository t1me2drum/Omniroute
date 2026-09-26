using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Collections.Concurrent;

namespace Omniroute.Api;

/// <summary>
/// MQTT клієнт для підключення до EcoFlow брокера
/// Підтримує автоматичне перепідключення та повторну підписку на топіки
/// </summary>
public class MqttLink : IDisposable
{
    private readonly MqttCredentials _credentials;
    private readonly string _userId;
    private readonly IManagedMqttClient _client;
    private readonly ConcurrentDictionary<string, byte> _topics = new();
    private readonly Action<string, byte[]> _onMessage;
    private readonly Action<bool> _onConnected;
    private readonly Action<string> _onFatal;

    public bool IsConnected => _client.IsConnected;

    public MqttLink(
        MqttCredentials credentials,
        string userId,
        Action<string, byte[]> onMessage,
        Action<bool> onConnected,
        Action<string> onFatal)
    {
        _credentials = credentials;
        _userId = userId;
        _onMessage = onMessage;
        _onConnected = onConnected;
        _onFatal = onFatal;

        _client = new MqttFactory().CreateManagedMqttClient();

        // Обробники подій
        _client.ConnectedAsync += OnConnectedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.ConnectingFailedAsync += OnConnectingFailedAsync;
    }

    /// <summary>
    /// Підключитися до MQTT брокера
    /// </summary>
    public async Task ConnectAsync(IEnumerable<string> initialTopics)
    {
        // Зберегти початкові топіки
        foreach (var topic in initialTopics)
        {
            _topics.TryAdd(topic, 0);
        }

        // Створити ClientId у форматі офіційного застосунку
        var clientId = $"WINDOWS_{Guid.NewGuid():N}".ToUpperInvariant() + $"_{_userId}";

        // Налаштування підключення
        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_credentials.Host, _credentials.Port)
            .WithCredentials(_credentials.Username, _credentials.Password)
            .WithClientId(clientId)
            .WithTls()
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .Build();

        var managedOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(clientOptions)
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .Build();

        await _client.StartAsync(managedOptions);
    }

    /// <summary>
    /// Підписатися на нові топіки
    /// </summary>
    public async Task SubscribeAsync(IEnumerable<string> newTopics)
    {
        var added = new List<string>();
        foreach (var topic in newTopics)
        {
            if (_topics.TryAdd(topic, 0))
            {
                added.Add(topic);
            }
        }

        if (added.Count > 0 && _client.IsConnected)
        {
            await _client.SubscribeAsync(added.Select(t =>
                new MqttTopicFilterBuilder()
                    .WithTopic(t)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
            ));
        }
    }

    /// <summary>
    /// Відписатися від топіків
    /// </summary>
    public async Task UnsubscribeAsync(IEnumerable<string> oldTopics)
    {
        var removed = new List<string>();
        foreach (var topic in oldTopics)
        {
            if (_topics.TryRemove(topic, out _))
            {
                removed.Add(topic);
            }
        }

        if (removed.Count > 0 && _client.IsConnected)
        {
            await _client.UnsubscribeAsync(removed);
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
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.EnqueueAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MQTT publish failed: {ex.Message}");
            return false;
        }
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MQTT connected");
        _onConnected(true);

        // Повторно підписатися на всі топіки
        var allTopics = _topics.Keys.ToList();
        if (allTopics.Count > 0)
        {
            await _client.SubscribeAsync(allTopics.Select(t =>
                new MqttTopicFilterBuilder()
                    .WithTopic(t)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
            ));
        }
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"MQTT disconnected: {e.Reason}");
        _onConnected(false);
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

    private Task OnConnectingFailedAsync(ConnectingFailedEventArgs e)
    {
        var exception = e.Exception;
        System.Diagnostics.Debug.WriteLine($"MQTT connection failed: {exception?.Message}");

        // Перевірка на помилки автентифікації
        if (exception?.Message?.Contains("authentication", StringComparison.OrdinalIgnoreCase) == true ||
            exception?.Message?.Contains("not authorized", StringComparison.OrdinalIgnoreCase) == true)
        {
            _onFatal("MQTT: доступ заборонено");
        }
        else
        {
            _onFatal($"MQTT: {exception?.Message ?? "не вдалося підключитися"}");
        }

        return Task.CompletedTask;
    }

    public async void Dispose()
    {
        if (_client.IsConnected)
        {
            await _client.StopAsync();
        }
        _client?.Dispose();
    }
}
