namespace SPHAssistant.Line.Core.Interfaces;

public interface ILineWebhookHandler
{
    Task HandleAsync(string requestBody, string signature);
}
