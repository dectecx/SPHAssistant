using Microsoft.AspNetCore.Mvc;
using SPHAssistant.Line.Core.Interfaces;

namespace SPHAssistant.Line.Webhook.Controllers;

[ApiController]
[Route("api/line/webhook")]
public class LineWebhookController : ControllerBase
{
    private readonly ILogger<LineWebhookController> _logger;
    private readonly ILineWebhookHandler _webhookHandler;

    public LineWebhookController(ILogger<LineWebhookController> logger, ILineWebhookHandler webhookHandler)
    {
        _logger = logger;
        _webhookHandler = webhookHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        try
        {
            var signature = Request.Headers["X-Line-Signature"].ToString();
            
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            _logger.LogInformation("Received webhook. Signature: {Signature}, Body: {Body}", signature, requestBody);
            
            await _webhookHandler.HandleAsync(requestBody, signature);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook.");
            // In case of an error, still return OK to Line Platform to avoid retries.
            // The error is logged for internal investigation.
            return Ok();
        }
    }
}
