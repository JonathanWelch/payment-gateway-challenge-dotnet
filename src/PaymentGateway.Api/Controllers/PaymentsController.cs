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
    private readonly PaymentsRepository _paymentsRepository;

    public PaymentsController(PaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> CreatePaymentAsync([FromBody] PostPaymentRequest paymentRequest)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var response = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = paymentRequest.ExpiryYear,
            ExpiryMonth = paymentRequest.ExpiryMonth,
            Amount = paymentRequest.Amount,
            CardNumberLastFour = Int32.Parse(paymentRequest.CardNumber[^4..]),
            Currency = paymentRequest.Currency,
            Status = PaymentStatus.Authorized
        };

        return new ObjectResult(response)
        {
            StatusCode = (int?)HttpStatusCode.Created
        };
    }
}