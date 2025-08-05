using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Models.Requests;

[FutureExpiryDate]
public class PostPaymentRequest
{
    [Required(ErrorMessage = "Card number is required.")]
    [StringLength(19, MinimumLength = 14, ErrorMessage = "Card number must be between 14-19 characters long.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Card number must only contain numeric characters.")]
    public string CardNumber { get; set; }

    [Required(ErrorMessage = "Expiry month is required.")]
    [Range(1, 12, ErrorMessage = "Expiry month must be between 1-12.")]
    public int ExpiryMonth { get; set; }

    [Required(ErrorMessage = "Expiry year is required.")]
    public int ExpiryYear { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [AllowedValues("GBP", "USD", "EUR", ErrorMessage = "Currency must be GBP, USD or EUR.")]
    public string Currency { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be an integer greater than 0.")]
    public int Amount { get; set; }

    [Required(ErrorMessage = "CVV is required.")]
    [Range(100, 9999, ErrorMessage = "CVV must be 3-4 characters long.")]
    public int Cvv { get; set; }
}