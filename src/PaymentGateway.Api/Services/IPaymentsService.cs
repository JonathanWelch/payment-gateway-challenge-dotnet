using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services
{
    public interface IPaymentsService
    {
        Task<PostPaymentResponse?> GetPaymentAsync(Guid id);
        Task<PostPaymentResponse> CreatePaymentAsync(PostPaymentRequest paymentRequest);
    }
}
