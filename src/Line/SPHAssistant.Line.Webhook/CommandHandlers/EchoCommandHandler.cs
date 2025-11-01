using isRock.LineBot;
using SPHAssistant.Line.Core.Interfaces;

namespace SPHAssistant.Line.Webhook.CommandHandlers;

public class EchoCommandHandler : ICommandHandler
{
    public string Command => "echo";

    public Task<IEnumerable<MessageBase>?> HandleAsync(string[] args)
    {
        if (args.Length == 0)
        {
            return Task.FromResult<IEnumerable<MessageBase>?>(new[] { new TextMessage("請提供要回應的文字。 用法: !echo [文字]") });
        }

        var echoMessage = string.Join(" ", args);
        IEnumerable<MessageBase> messages = new[] { new TextMessage(echoMessage) };

        return Task.FromResult<IEnumerable<MessageBase>?>(messages);
    }
}
