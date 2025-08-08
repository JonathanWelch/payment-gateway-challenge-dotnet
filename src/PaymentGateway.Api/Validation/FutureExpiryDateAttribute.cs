using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validation;

public class FutureExpiryDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return true;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not PostPaymentRequest request)
        {
            return new ValidationResult("Invalid object type for expiry date validation.");
        }

        var timeProvider = (TimeProvider?)validationContext.GetService(typeof(TimeProvider)) ?? TimeProvider.System;
        var currentDate = timeProvider.GetLocalNow().DateTime;
        var currentYear = currentDate.Year;
        var currentMonth = currentDate.Month;

        if ((request.ExpiryYear < currentYear) || (request.ExpiryYear == currentYear && request.ExpiryMonth <= currentMonth))
        {
            return new ValidationResult("Card expiry must be in the future.", new[] { nameof(request.ExpiryYear) });
        }

        return ValidationResult.Success;
    }
}