using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Core;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Integration.Controllers;

[TestFixture]
public class PaymentsControllerTests
{
    private readonly Random _random = new();
    private PaymentsRepository _paymentsRepository = null!;
    private WebApplicationFactory<PaymentsController> _webApplicationFactory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        var mockBankService = new Mock<IAcquiringBank>();
        var authorizationCode = Guid.NewGuid().ToString();

        mockBankService
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Authorized = true,
                AuthorizationCode = authorizationCode
            });

        _paymentsRepository = new PaymentsRepository();
        _webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        _client = _webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton<IPaymentsRepository>(_paymentsRepository)
                    .AddScoped<IPaymentsService, PaymentsService>()
                    .AddSingleton(mockBankService.Object)))
            .CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _webApplicationFactory.Dispose();
    }

    [Test]
    public async Task CanRetrievePayment_WithCorrectDetails()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999),
            Currency = "GBP",
            Status = PaymentStatus.Authorized
        };

        _paymentsRepository.Add(payment);

        // Act
        var response = await _client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(paymentResponse, Is.Not.Null);
        Assert.That(paymentResponse.Amount, Is.EqualTo(payment.Amount));
        Assert.That(paymentResponse.CardNumberLastFour, Is.EqualTo(payment.CardNumberLastFour));
        Assert.That(paymentResponse.ExpiryMonth, Is.EqualTo(payment.ExpiryMonth));
        Assert.That(paymentResponse.ExpiryYear, Is.EqualTo(payment.ExpiryYear));
        Assert.That(paymentResponse.Currency, Is.EqualTo(payment.Currency));
        Assert.That(paymentResponse.Status, Is.EqualTo(payment.Status));
    }

    [Test]
    public async Task Returns404IfPaymentNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase("GBP")]
    [TestCase("USD")]
    [TestCase("EUR")]
    public async Task CanCreatePayment_WithCorrectDetails(string validCurrency)
    {
        // Arrange
        var paymentRequest = CreateValidPaymentRequest(currency: validCurrency);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", paymentRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(paymentResponse, Is.Not.Null);
        Assert.That(paymentResponse!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(paymentResponse.Amount, Is.EqualTo(paymentRequest.Amount));
        int expectedCardNumberLastFour = 5678;
        Assert.That(paymentResponse.CardNumberLastFour, Is.EqualTo(expectedCardNumberLastFour));
        Assert.That(paymentResponse.ExpiryMonth, Is.EqualTo(paymentRequest.ExpiryMonth));
        Assert.That(paymentResponse.ExpiryYear, Is.EqualTo(paymentRequest.ExpiryYear));
        Assert.That(paymentResponse.Currency, Is.EqualTo(paymentRequest.Currency));
        Assert.That(paymentResponse.Status, Is.EqualTo(PaymentStatus.Authorized));
    }

    [TestCase("A")]
    [TestCase("1234567891123A")]
    public async Task Returns422IfCardNumberNotNumeric(string invalidCardNumber)
    {
        // Arrange
        var request = CreateValidPaymentRequest(cardNumber: invalidCardNumber);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "CardNumber", "Card number must only contain numeric characters.");
    }

    [TestCase("")]
    [TestCase("1")]
    [TestCase("1234567891123")]
    [TestCase("12345678911234567892")]

    public async Task Returns422IfCardNumberWrongLength(string invalidCardNumber)
    {
        // Arrange
        var request = CreateValidPaymentRequest(cardNumber: invalidCardNumber);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "CardNumber", "Card number must be between 14-19 characters long.");
    }

    [Test]
    public async Task Returns422IfCardNumberMissing()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "CardNumber", "Card number is required.");

    }

    [Test]
    public async Task Returns422IfExpiryMonthMissing()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "ExpiryMonth", "Expiry month must be between 1-12.");
    }

    [TestCase(0)]
    [TestCase(13)]
    [TestCase(111)]
    public async Task Returns422IfExpiryMonthInvalid(int invalidExpiryMonth)
    {
        // Arrange
        var request = CreateValidPaymentRequest(expiryMonth: invalidExpiryMonth);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "ExpiryMonth", "Expiry month must be between 1-12.");
    }

    [TestCase("EGP")]
    [TestCase("LRD")]
    [TestCase("MAD")]
    public async Task Returns422IfCurrencyInvalid(string invalidCurrency)
    {
        // Arrange
        var request = CreateValidPaymentRequest(currency: invalidCurrency);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "Currency", "Currency must be GBP, USD or EUR.");
    }

    [Test]
    public async Task Returns422IfAmountInvalid()
    {
        // Arrange
        var request = CreateValidPaymentRequest(amount: 0);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "Amount", "Amount must be an integer greater than 0.");
    }

    [Test]
    public async Task Returns422IfAmountMissing()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Cvv = 123
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "Amount", "Amount must be an integer greater than 0.");
    }

    [Test]
    public async Task Returns422IfCardExpired()
    {
        // Arrange
        var monthAgo = DateTime.Now.AddMonths(-1);
        var request = CreateValidPaymentRequest(expiryMonth : monthAgo.Month, expiryYear: monthAgo.Year);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "ExpiryYear", "Card expiry must be in the future.");
    }

    [Test]
    public async Task Returns422IfCardExpiresThisMonth()
    {
        // Arrange
        var currentDate = DateTime.Now;
        var request = CreateValidPaymentRequest(expiryMonth: currentDate.Month, expiryYear: currentDate.Year);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        await AssertValidationErrorAsync(response, "ExpiryYear", "Card expiry must be in the future.");
    }

    [Test]
    public async Task CanCreateAndRetrievePayment_WithCorrectDetails()
    {
        // Arrange
        var paymentRequest = CreateValidPaymentRequest();

        // Act
        var httpPostResponse = await _client.PostAsJsonAsync($"/api/Payments", paymentRequest);
        var postPaymentResponse = await httpPostResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.That(postPaymentResponse, Is.Not.Null);
        var httpGetResponse = await _client.GetAsync($"/api/Payments/{postPaymentResponse!.Id}");
        var paymentResponse = await httpGetResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.That(httpPostResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(httpGetResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(paymentResponse, Is.Not.Null);
        Assert.That(paymentResponse!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(paymentResponse.Amount, Is.EqualTo(paymentRequest.Amount));
        int expectedCardNumberLastFour = 5678;
        Assert.That(paymentResponse.CardNumberLastFour, Is.EqualTo(expectedCardNumberLastFour));
        Assert.That(paymentResponse.ExpiryMonth, Is.EqualTo(paymentRequest.ExpiryMonth));
        Assert.That(paymentResponse.ExpiryYear, Is.EqualTo(paymentRequest.ExpiryYear));
        Assert.That(paymentResponse.Currency, Is.EqualTo(paymentRequest.Currency));
        Assert.That(paymentResponse.Status, Is.EqualTo(PaymentStatus.Authorized));
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

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string fieldName, string expectedMessage)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));

        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key(fieldName));
        var fieldErrors = errorResponse![fieldName];
        Assert.That(fieldErrors, Contains.Item(expectedMessage));
    }
}