using isRock.LineBot;

namespace SPHAssistant.Line.Core.Interfaces;

public interface ILinePushService
{
    Task PushAsync(string to, string message);
    Task PushAsync(string to, IEnumerable<MessageBase> messages);
    Task MulticastAsync(IEnumerable<string> to, string message);
    Task MulticastAsync(IEnumerable<string> to, IEnumerable<MessageBase> messages);
}
