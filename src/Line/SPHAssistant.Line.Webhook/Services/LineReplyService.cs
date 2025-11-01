using isRock.LineBot;
using SPHAssistant.Line.Core.Interfaces;

namespace SPHAssistant.Line.Webhook.Services;

public class LineReplyService : ILineReplyService
{
    private readonly ILogger<LineReplyService> _logger;
    private readonly Bot _lineBot;

    public LineReplyService(ILogger<LineReplyService> logger, Bot lineBot)
    {
        _logger = logger;
        _lineBot = lineBot;
    }

    public Task ReplyAsync(string replyToken, string message)
    {
        try
        {
            _lineBot.ReplyMessage(replyToken, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reply. ReplyToken: {ReplyToken}", replyToken);
        }
        return Task.CompletedTask;
    }

    public Task ReplyAsync(string replyToken, IEnumerable<MessageBase> messages)
    {
        try
        {
            _lineBot.ReplyMessage(replyToken, messages.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reply. ReplyToken: {ReplyToken}", replyToken);
        }
        return Task.CompletedTask;
    }
}
