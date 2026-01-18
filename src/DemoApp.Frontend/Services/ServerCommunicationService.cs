using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace DemoApp.Frontend.Services;

public class ServerCommunicationService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Uri _backendBaseUri;
    private readonly ILogger<ServerCommunicationService> _logger;

    private HubConnection? _hubConnection;

    public event Func<string, Task>? MessageReceived;
    public event Func<string, Task>? ErrorOccurred;

    public ServerCommunicationService(HttpClient httpClient, Uri backendBaseUri, ILogger<ServerCommunicationService> logger)
    {
        _httpClient = httpClient;
        _backendBaseUri = backendBaseUri;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null)
        {
            return;
        }

        var hubUrl = new Uri(_backendBaseUri, "hubs/messages");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("ReceiveMessage", async message =>
        {
            _logger.LogInformation("Received message from hub: {Message}", message);

            if (MessageReceived is not null)
            {
                await MessageReceived.Invoke(message);
            }
        });

        _hubConnection.Closed += async error =>
        {
            _logger.LogWarning(error, "SignalR connection closed.");
            if (ErrorOccurred is not null)
            {
                await ErrorOccurred.Invoke("Подключение к серверу потеряно.");
            }
        };

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "SignalR reconnecting...");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected. ConnectionId: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };

        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("SignalR connection started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection.");
            if (ErrorOccurred is not null)
            {
                await ErrorOccurred.Invoke("Не удалось подключиться к серверу по SignalR.");
            }
        }
    }

    public async Task<bool> SendMessageAsync(string text, int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var request = new { Text = text };

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/messages", request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                _logger.LogWarning("Server returned non-success status code: {StatusCode}", response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed on attempt {Attempt}.", attempt);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "HTTP request canceled or timed out on attempt {Attempt}.", attempt);
            }

            if (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        if (ErrorOccurred is not null)
        {
            await ErrorOccurred.Invoke("Сервер недоступен или не отвечает.");
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
