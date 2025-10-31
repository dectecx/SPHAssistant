using Line.Bot.SDK.Model.Message;

namespace SPHAssistant.Line.Core.Interfaces;

public interface ICommandHandler
{
    string Command { get; }
    Task<IEnumerable<ISendMessage>?> HandleAsync(string[] args);
}
