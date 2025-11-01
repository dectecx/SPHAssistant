using isRock.LineBot;
using Microsoft.Extensions.Logging;
using SPHAssistant.Line.Core.Interfaces;
using System.Net;

namespace SPHAssistant.Line.Webhook.Handlers;

public class LineWebhookHandler : ILineWebhookHandler
{
    private readonly ILogger<LineWebhookHandler> _logger;
    private readonly Bot _lineBot;
    private readonly ICommandRouter _commandRouter;
    private readonly ILineReplyService _replyService;
    private readonly string _channelSecret;

    public LineWebhookHandler(
        ILogger<LineWebhookHandler> logger,
        Bot lineBot,
        ICommandRouter commandRouter,
        ILineReplyService replyService,
        IConfiguration configuration)
    {
        _logger = logger;
        _lineBot = lineBot;
        _commandRouter = commandRouter;
        _replyService = replyService;
        _channelSecret = configuration["LineBot:ChannelSecret"] ?? throw new InvalidOperationException("LineBot ChannelSecret is not configured.");
    }

    public async Task HandleAsync(string requestBody, string signature)
    {
        try
        {
            // 2. Parse events
            var receivedMessage = Utility.Parsing(requestBody);

            foreach (var evt in receivedMessage.events)
            {
                _logger.LogInformation("Received event type: {EventType}", evt.type);
                
                // 3. Dispatch events
                if (evt.type == "message" && evt.message.type == "text")
                {
                    await HandleTextMessageAsync(evt.replyToken, evt.message.text);
                }
                else
                {
                    _logger.LogInformation("Unhandled event type: {EventType} or message type: {MessageType}", evt.type, evt.message?.type);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while handling the webhook.");
        }
    }

    private async Task HandleTextMessageAsync(string replyToken, string text)
    {
        var replyMessages = await _commandRouter.RouteAsync(text);
        if (replyMessages != null && replyMessages.Any())
        {
            await _replyService.ReplyAsync(replyToken, replyMessages);
        }
        else
        {
            _logger.LogInformation("Received a non-command text message or a command with no reply.");
        }
    }
}
