using System.Net;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentsService _paymentsService;

    public PaymentsController(IPaymentsService paymentsService)
    {
        _paymentsService = paymentsService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = await _paymentsService.GetPaymentAsync(id);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> CreatePaymentAsync([FromBody] PostPaymentRequest paymentRequest)
    {
        var paymentResponse = await _paymentsService.CreatePaymentAsync(paymentRequest);

        if (paymentResponse.Status == PaymentStatus.Rejected)
        {
            return Problem(
                title: "Payment rejected",
                detail: "The payment could not be processed.",
                statusCode: StatusCodes.Status502BadGateway
            );
        }

        return new ObjectResult(paymentResponse)
        {
            StatusCode = (int?)HttpStatusCode.Created
        };
    }
}