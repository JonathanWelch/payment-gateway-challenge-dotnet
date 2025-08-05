using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsService : IPaymentsService
{
    private readonly PaymentsRepository _paymentsRepository;

    public PaymentsService(PaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;
    }

    public async Task<PostPaymentResponse?> GetPaymentAsync(Guid id)
    {
        return _paymentsRepository.Get(id);
    }

    public async Task<PostPaymentResponse> CreatePaymentAsync(PostPaymentRequest paymentRequest)
    {
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

        _paymentsRepository.Add(response);

        return response;
    }
}