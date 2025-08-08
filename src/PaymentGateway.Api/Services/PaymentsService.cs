using PaymentGateway.Api.Core;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsService : IPaymentsService
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IAcquiringBank _acquiringBank;

    public PaymentsService(IPaymentsRepository paymentsRepository, IAcquiringBank acquiringBank)
    {
        _paymentsRepository = paymentsRepository;
        _acquiringBank = acquiringBank;
    }

    public async Task<PostPaymentResponse?> GetPaymentAsync(Guid id)
    {
        return _paymentsRepository.Get(id);
    }

    public async Task<PostPaymentResponse> CreatePaymentAsync(PostPaymentRequest paymentRequest)
    {
        var request = new PaymentRequest()
        {
            Amount = paymentRequest.Amount,
            CardNumber = paymentRequest.CardNumber,
            Currency = paymentRequest.Currency,
            ExpiryDate = $"{paymentRequest.ExpiryMonth:D2}/{paymentRequest.ExpiryYear}",
            Cvv = paymentRequest.Cvv
        };

        PaymentResult paymentResult = await _acquiringBank.ProcessPaymentAsync(request);

        PaymentStatus status = paymentResult.IsSuccess
            ? paymentResult.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined
            : PaymentStatus.Rejected;

        var response = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = paymentRequest.ExpiryYear,
            ExpiryMonth = paymentRequest.ExpiryMonth,
            Amount = paymentRequest.Amount,
            CardNumberLastFour = int.Parse(paymentRequest.CardNumber[^4..]),
            Currency = paymentRequest.Currency,
            Status = status
        };

        if (status == PaymentStatus.Rejected)
        {
            return response;
        }

        _paymentsRepository.Add(response);

        return response;
    }
}