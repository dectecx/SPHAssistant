using Line.Bot.SDK.Model;

namespace SPHAssistant.Line.Core.Interfaces;

public interface ILinePushService
{
    Task PushAsync(string to, string message);
    Task PushAsync(string to, IEnumerable<ISendMessage> messages);
    Task MulticastAsync(IEnumerable<string> to, string message);
    Task MulticastAsync(IEnumerable<string> to, IEnumerable<ISendMessage> messages);
}
