using System.Text.Json.Serialization;

using PaymentGateway.Api.Core;

namespace PaymentGateway.Api.Infrastructure;

internal class AcquiringBankPaymentRequest
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; init; }

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; }

    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    [JsonPropertyName("cvv")]
    public string Cvv { get; init; }

    public AcquiringBankPaymentRequest(PaymentRequest request)
    {
        CardNumber = request.CardNumber;
        ExpiryDate = request.ExpiryDate;
        Currency = request.Currency;
        Amount = request.Amount;
        Cvv = request.Cvv;
    }
}