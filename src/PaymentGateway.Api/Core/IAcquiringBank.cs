namespace PaymentGateway.Api.Core
{
    public interface IAcquiringBank
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    }
}
