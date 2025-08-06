using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Infrastructure;

internal class AcquiringBankPaymentResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }
    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; set; }
}