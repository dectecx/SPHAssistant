using isRock.LineBot;

namespace SPHAssistant.Line.Core.Interfaces;

public interface ICommandHandler
{
    string Command { get; }
    Task<IEnumerable<MessageBase>?> HandleAsync(string[] args);
}
