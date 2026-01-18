using DemoApp.Backend.Hubs;
using DemoApp.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DemoApp.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IHubContext<MessageHub> _hubContext;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IHubContext<MessageHub> hubContext, ILogger<MessagesController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text must not be empty.");
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var responseText = $"[{receivedAt:O}] {request.Text}";

        _logger.LogInformation("Processing message at {Time}: {Text}", receivedAt, request.Text);

        // Simulate async work and ensure non-blocking behavior
        await Task.Yield();

        await _hubContext.Clients.All.SendAsync("ReceiveMessage", responseText, cancellationToken);

        // HTTP response does not contain the payload itself; client receives it via SignalR
        return Accepted();
    }
}
