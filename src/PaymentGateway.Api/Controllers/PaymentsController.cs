using System.Net;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

/// <summary>
/// Handles payment processing operations for the payment gateway
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentsService _paymentsService;

    public PaymentsController(IPaymentsService paymentsService)
    {
        _paymentsService = paymentsService;
    }

    /// <summary>
    /// Retrieves a payment by its unique payment identifier
    /// </summary>
    /// <param name="id">The unique payment identifier</param>
    /// <returns>The payment details if found</returns>
    /// <response code="200">Payment found and returned successfully</response>
    /// <response code="404">Payment not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = await _paymentsService.GetPaymentAsync(id);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    /// <summary>
    /// Creates a new payment request
    /// </summary>
    /// <param name="paymentRequest">The payment details</param>
    /// <returns>The details and status of the requested payment</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/payments
    ///     {
    ///         "cardNumber": "4111111111111111",
    ///         "expiryMonth": 12,
    ///         "expiryYear": 2025,
    ///         "currency": "GBP",
    ///         "amount": 1000,
    ///         "cvv":"123"
    ///     }
    /// 
    /// Amount should be in the minor currency unit.  For example, if the currency was USD then $0.01 would be supplied as 1 and $10.50 would be supplied as 1050.
    /// Currency can only be set to one of the three currently supported currencies of GBP, USD and EUR.
    /// </remarks>
    /// <response code="201">Payment created successfully and sent to acquiring bank</response>
    /// <response code="422">Validation errors in the payment request</response>
    /// <response code="502">Payment processing failed - could not communicate with acquiring bank</response>
    /// <response code="400">Invalid request format</response>
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
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