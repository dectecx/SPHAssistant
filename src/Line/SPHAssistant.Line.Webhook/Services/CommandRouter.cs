using isRock.LineBot;
using SPHAssistant.Line.Core.Interfaces;
using System.Text.RegularExpressions;

namespace SPHAssistant.Line.Webhook.Services;

public class CommandRouter : ICommandRouter
{
    private const string CommandPrefix = "!";
    private readonly ILogger<CommandRouter> _logger;
    private readonly IEnumerable<ICommandHandler> _commandHandlers;
    private readonly Dictionary<string, ICommandHandler> _handlerMap;

    public CommandRouter(ILogger<CommandRouter> logger, IEnumerable<ICommandHandler> commandHandlers)
    {
        _logger = logger;
        _commandHandlers = commandHandlers;
        _handlerMap = _commandHandlers.ToDictionary(h => h.Command.ToLowerInvariant());
    }

    public async Task<IEnumerable<MessageBase>?> RouteAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith(CommandPrefix))
        {
            return null;
        }

        var parts = Regex.Split(text.Substring(CommandPrefix.Length).Trim(), @"\s+");
        var command = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        if (_handlerMap.TryGetValue(command, out var handler))
        {
            _logger.LogInformation("Routing command '{Command}' to handler '{HandlerType}'.", command, handler.GetType().Name);
            try
            {
                return await handler.HandleAsync(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command handler for command '{Command}'.", command);
                // Optionally return a generic error message
                return new[] { new TextMessage($"執行指令 '{command}' 時發生錯誤。") };
            }
        }

        _logger.LogWarning("No command handler found for command '{Command}'.", command);
        return new[] { new TextMessage($"找不到指令 '{command}'。") };
    }
}
