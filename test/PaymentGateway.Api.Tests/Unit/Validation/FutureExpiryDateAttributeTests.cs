using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validation;
using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Tests.Unit.Validation;

[TestFixture]
public class FutureExpiryDateAttributeTests
{
    private FutureExpiryDateAttribute _attribute = null!;

    [SetUp]
    public void SetUp()
    {
        _attribute = new FutureExpiryDateAttribute();
    }

    [Test]
    public void GetValidationResult_WhenExpiryInFuture_ReturnsSuccess()
    {
        // Arrange
        var request = CreateValidPaymentRequest(expiryMonth: 12, expiryYear: DateTime.Now.Year + 1);
        var context = new ValidationContext(request);

        // Act
        var result = _attribute.GetValidationResult(request, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void GetValidationResult_WhenExpiryInPast_ReturnsError()
    {
        // Arrange
        var request = CreateValidPaymentRequest(expiryMonth: 12, expiryYear: DateTime.Now.Year - 1);
        var context = new ValidationContext(request);

        // Act
        var result = _attribute.GetValidationResult(request, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        Assert.That(result?.ErrorMessage, Is.EqualTo("Card expiry must be in the future."));
    }

    [Test]
    public void GetValidationResult_WhenExpiryInCurrentMonth_ReturnsError()
    {
        // Arrange
        var currentDate = DateTime.Now;
        var request = CreateValidPaymentRequest(expiryMonth: currentDate.Month, expiryYear: currentDate.Year);
        var context = new ValidationContext(request);

        // Act
        var result = _attribute.GetValidationResult(request, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        Assert.That(result?.ErrorMessage, Is.EqualTo("Card expiry must be in the future."));
    }


    [Test]
    public void GetValidationResult_WhenInvalidRequestObject_ReturnsError()
    {
        // Arrange
        var currentDate = DateTime.Now;
        var request = CreateValidPaymentRequest(expiryMonth: currentDate.Month, expiryYear: currentDate.Year);
        var context = new ValidationContext(request);

        // Act
        var result = _attribute.GetValidationResult(null, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        Assert.That(result?.ErrorMessage, Is.EqualTo("Invalid object type for expiry date validation."));
    }

    private static PostPaymentRequest CreateValidPaymentRequest(
        string cardNumber = "1234567812345678",
        int expiryMonth = 12,
        int? expiryYear = null,
        string currency = "GBP",
        int amount = 1000,
        int cvv = 123)
    {
        return new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear ?? DateTime.Now.Year + 1,
            Currency = currency,
            Amount = amount,
            Cvv = cvv
        };
    }
}
