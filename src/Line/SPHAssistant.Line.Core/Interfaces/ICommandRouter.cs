using isRock.LineBot;

namespace SPHAssistant.Line.Core.Interfaces;

public interface ICommandRouter
{
    Task<IEnumerable<MessageBase>?> RouteAsync(string text);
}
