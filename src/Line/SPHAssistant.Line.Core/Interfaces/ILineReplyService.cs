using Line.Bot.SDK.Model;

namespace SPHAssistant.Line.Core.Interfaces;

public interface ILineReplyService
{
    Task ReplyAsync(string replyToken, string message);
    Task ReplyAsync(string replyToken, IEnumerable<ISendMessage> messages);
}
