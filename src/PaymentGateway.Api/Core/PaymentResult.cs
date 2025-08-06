namespace PaymentGateway.Api.Core;

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public bool Authorized { get; set; }
    public string AuthorizationCode { get; set; }
}