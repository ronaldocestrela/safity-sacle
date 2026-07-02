using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Application.Abstractions.Billing;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/stripe/webhook")]
public sealed class StripeWebhookController(
    IBillingService billingService,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature))
        {
            return BadRequest(new { message = "Missing Stripe-Signature header." });
        }

        var result = await billingService.ProcessWebhookAsync(json, signature, cancellationToken);

        if (result.Status is ProcessWebhookStatus.InvalidSignature or ProcessWebhookStatus.ProcessingFailed)
        {
            logger.LogWarning(
                "Stripe webhook rejected. Status={Status} Message={Message}",
                result.Status,
                result.ErrorMessage);
        }

        return result.Status switch
        {
            ProcessWebhookStatus.Success or ProcessWebhookStatus.AlreadyProcessed or ProcessWebhookStatus.Ignored =>
                Ok(),

            ProcessWebhookStatus.InvalidSignature =>
                BadRequest(new { message = result.ErrorMessage ?? "Invalid signature." }),

            ProcessWebhookStatus.ProcessingFailed =>
                BadRequest(new { message = result.ErrorMessage ?? "Webhook processing failed." }),

            _ => BadRequest(new { message = "Unexpected webhook status." }),
        };
    }
}
