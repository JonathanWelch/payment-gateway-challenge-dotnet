using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

[TestFixture]
public class PaymentsControllerTests
{
    private readonly Random _random = new();
    
    [Test]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(paymentResponse, Is.Not.Null);
    }

    [Test]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase("GBP")]
    [TestCase("USD")]
    [TestCase("EUR")]
    public async Task CreatesPaymentSuccessfully(string validCurrency)
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var paymentRequest = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = validCurrency,
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", paymentRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(paymentResponse, Is.Not.Null);
        Assert.That(paymentResponse.Id, Is.Not.EqualTo(Guid.Empty));
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
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = invalidCardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("CardNumber"));
        var cardNumberErrors = errorResponse["CardNumber"];
        Assert.That(cardNumberErrors, Contains.Item("Card number must only contain numeric characters."));
    }

    [TestCase("")]
    [TestCase("1")]
    [TestCase("1234567891123")]
    [TestCase("12345678911234567892")]

    public async Task Returns422IfCardNumberWrongLength(string invalidCardNumber)
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = invalidCardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("CardNumber"));
        var cardNumberErrors = errorResponse["CardNumber"];
        Assert.That(cardNumberErrors, Contains.Item("Card number must be between 14-19 characters long."));
    }

    [Test]
    public async Task Returns422IfCardNumberMissing()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("CardNumber"));
        var cardNumberErrors = errorResponse["CardNumber"];
        Assert.That(cardNumberErrors, Contains.Item("Card number is required."));
    }

    [Test]
    public async Task Returns422IfExpiryMonthMissing()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("ExpiryMonth"));
        var cardNumberErrors = errorResponse["ExpiryMonth"];
        Assert.That(cardNumberErrors, Contains.Item("Expiry month must be between 1-12."));
    }

    [TestCase(0)]
    [TestCase(13)]
    [TestCase(111)]
    public async Task Returns422IfExpiryMonthInvalid(int invalidExpiryMonth)
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = invalidExpiryMonth,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("ExpiryMonth"));
        var cardNumberErrors = errorResponse["ExpiryMonth"];
        Assert.That(cardNumberErrors, Contains.Item("Expiry month must be between 1-12."));
    }

    [TestCase("EGP")]
    [TestCase("LRD")]
    [TestCase("MAD")]
    public async Task Returns422IfCurrencyInvalid(string invalidCurrency)
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = invalidCurrency,
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("Currency"));
        var cardNumberErrors = errorResponse["Currency"];
        Assert.That(cardNumberErrors, Contains.Item("Currency must be GBP, USD or EUR."));
    }

    [Test]
    public async Task Returns422IfAmountInvalid()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 0,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("Amount"));
        var cardNumberErrors = errorResponse["Amount"];
        Assert.That(cardNumberErrors, Contains.Item("Amount must be an integer greater than 0."));
    }


    [Test]
    public async Task Returns422IfAmountMissing()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("Amount"));
        var cardNumberErrors = errorResponse["Amount"];
        Assert.That(cardNumberErrors, Contains.Item("Amount must be an integer greater than 0."));
    }

    [Test]
    public async Task Returns422IfCardExpired()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var monthAgo = DateTime.Now.AddMonths(-1);
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = monthAgo.Month,
            ExpiryYear = monthAgo.Year, 
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("ExpiryYear"));
        var expiryErrors = errorResponse["ExpiryYear"];
        Assert.That(expiryErrors, Contains.Item("Card expiry must be in the future."));
    }

    [Test]
    public async Task Returns422IfCardExpiresThisMonth()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var currentDate = DateTime.Now;
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = currentDate.Month,
            ExpiryYear = currentDate.Year,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", request);
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse, Contains.Key("ExpiryYear"));
        var expiryErrors = errorResponse["ExpiryYear"];
        Assert.That(expiryErrors, Contains.Item("Card expiry must be in the future."));
    }
}