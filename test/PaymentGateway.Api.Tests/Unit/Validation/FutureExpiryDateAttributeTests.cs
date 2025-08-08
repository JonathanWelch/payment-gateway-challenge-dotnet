using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validation;
using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Time.Testing;

namespace PaymentGateway.Api.Tests.Unit.Validation;

[TestFixture]
public class FutureExpiryDateAttributeTests
{
    private static readonly DateTimeOffset TestCurrentDate = new(new DateTime(2025, 1, 1), TimeSpan.Zero);
    private const int TestCurrentYear = 2025;
    private const int TestCurrentMonth = 1;
    private const int TestValidFutureYear = 2025;
    private const int TestPastYear = 2024;
    private const int TestDecemberMonth = 12;

    private const string ExpectedFutureExpiryError = "Card expiry must be in the future.";
    private const string ExpectedInvalidObjectError = "Invalid object type for expiry date validation.";

    private FutureExpiryDateAttribute _attribute = null!;
    private FakeTimeProvider _fakeTimeProvider = null!;
    private ServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeTimeProvider = new FakeTimeProvider();
        _attribute = new FutureExpiryDateAttribute();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(_fakeTimeProvider);
        _serviceProvider = services.BuildServiceProvider();
        _fakeTimeProvider.SetUtcNow(TestCurrentDate);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public void GetValidationResult_WhenExpiryInFuture_ReturnsSuccess()
    {
        // Arrange
        var request = CreateValidPaymentRequest(expiryMonth: TestDecemberMonth, expiryYear: TestValidFutureYear);

        // Act
        var result = ValidateRequestWithTimeProvider(request);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void GetValidationResult_WhenExpiryInPast_ReturnsError()
    {
        // Arrange
        var request = CreateValidPaymentRequest(expiryMonth: TestDecemberMonth, expiryYear: TestPastYear);

        // Act
        var result = ValidateRequestWithTimeProvider(request);

        // Assert
        AssertValidationError(result, ExpectedFutureExpiryError);
    }

    [Test]
    public void GetValidationResult_WhenExpiryInCurrentMonth_ReturnsError()
    {
        // Arrange
        var request = CreateValidPaymentRequest(expiryMonth: TestCurrentMonth, expiryYear: TestCurrentYear);

        // Act
        var result = ValidateRequestWithTimeProvider(request);

        // Assert
        AssertValidationError(result, ExpectedFutureExpiryError);
    }


    [Test]
    public void GetValidationResult_WhenInvalidRequestObject_ReturnsError()
    {
        // Arrange & Act
        var result = ValidateNullRequest();

        // Assert
        AssertValidationError(result, ExpectedInvalidObjectError);
    }

    private ValidationResult? ValidateRequestWithTimeProvider(PostPaymentRequest request)
    {
        var context = new ValidationContext(request, _serviceProvider, null);
        return _attribute.GetValidationResult(request, context);
    }

    private ValidationResult? ValidateNullRequest()
    {
        var dummyRequest = CreateValidPaymentRequest();
        var context = new ValidationContext(dummyRequest, _serviceProvider, null);
        return _attribute.GetValidationResult(null, context);
    }

    private static void AssertValidationError(ValidationResult? result, string expectedMessage)
    {
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        Assert.That(result?.ErrorMessage, Is.EqualTo(expectedMessage));
    }

    private static PostPaymentRequest CreateValidPaymentRequest(
        string cardNumber = "1234567812345678",
        int expiryMonth = TestDecemberMonth,
        int expiryYear = TestValidFutureYear,
        string currency = "GBP",
        int amount = 1000,
        string cvv = "123")
    {
        return new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            Currency = currency,
            Amount = amount,
            Cvv = cvv
        };
    }
}
