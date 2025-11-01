using isRock.LineBot;

namespace SPHAssistant.Line.Core.Interfaces;

public interface ILineReplyService
{
    Task ReplyAsync(string replyToken, string message);
    Task ReplyAsync(string replyToken, IEnumerable<MessageBase> messages);
}
