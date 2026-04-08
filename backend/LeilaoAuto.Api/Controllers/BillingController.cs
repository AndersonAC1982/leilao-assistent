using System.Text.Json;
using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpGet("plan")]
    [ProducesResponseType(typeof(BillingPlanResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentPlan(CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _billingService.GetCurrentPlanAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(BillingCheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout([FromBody] BillingCheckoutRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _billingService.StartCheckoutAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    [ProducesResponseType(typeof(BillingWebhookResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Webhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
        var response = await _billingService.HandleWebhookAsync(payload.GetRawText(), signature, cancellationToken);
        return Ok(response);
    }
}
