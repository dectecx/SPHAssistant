using Line.Bot.SDK;
using Line.Bot.SDK.Model.Internal;
using Microsoft.Extensions.Logging;
using SPHAssistant.Line.Core.Interfaces;
using System.Net;

namespace SPHAssistant.Line.Webhook.Handlers;

public class LineWebhookHandler : ILineWebhookHandler
{
    private readonly ILogger<LineWebhookHandler> _logger;
    private readonly ILineBot _lineBot;

    public LineWebhookHandler(ILogger<LineWebhookHandler> logger, ILineBot lineBot)
    {
        _logger = logger;
        _lineBot = lineBot;
    }

    public async Task HandleAsync(string requestBody, string signature)
    {
        try
        {
            var events = _lineBot.Parse(requestBody, signature);

            foreach (var evt in events)
            {
                _logger.LogInformation("Received event: {EventType}", evt.Type);
                switch (evt)
                {
                    // Handle other event types
                    default:
                        _logger.LogInformation("Unhandled event type: {EventType}", evt.Type);
                        break;
                }
            }
        }
        catch (LineBotException ex)
        {
            _logger.LogError(ex, "Error parsing Line webhook event.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while handling the webhook.");
        }
    }
}
