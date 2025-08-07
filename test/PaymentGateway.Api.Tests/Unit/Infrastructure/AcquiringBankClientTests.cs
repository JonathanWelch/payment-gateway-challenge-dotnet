using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using PaymentGateway.Api.Core;
using PaymentGateway.Api.Infrastructure;

namespace PaymentGateway.Api.Tests.Unit.Infrastructure;

[TestFixture]
public class AcquiringBankClientTests
{
    private Mock<IHttpClientFactory> _mockHttpClientFactory = null!;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private AcquiringBankClient _acquiringBankClient = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:8080/")
        };

        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory
            .Setup(x => x.CreateClient("AcquiringBankClient"))
            .Returns(_httpClient);

        _acquiringBankClient = new AcquiringBankClient(_mockHttpClientFactory.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task ProcessPaymentAsync_WhenBankReturnsAuthorized_ReturnsAuthorizedSuccessResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new
        {
            authorized = true,
            authorization_code = "eeb61d69-79f9-49fe-9547-5ea385dc2c5b"
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(bankResponse));

        // Act
        var result = await _acquiringBankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Authorized, Is.True);
        Assert.That(result.AuthorizationCode, Is.EqualTo("eeb61d69-79f9-49fe-9547-5ea385dc2c5b"));
    }

    [Test]
    public async Task ProcessPaymentAsync_WhenBankReturnsUnauthorized_ReturnsUnauthorizedSuccessResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new
        {
            authorized = false,
            authorization_code = ""
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(bankResponse));

        // Act
        var result = await _acquiringBankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Authorized, Is.False);
        Assert.That(result.AuthorizationCode, Is.EqualTo(""));
    }

    [TestCase(HttpStatusCode.BadRequest, "Bad Request")]
    [TestCase(HttpStatusCode.InternalServerError, "Internal Server Error")]
    public async Task ProcessPaymentAsync_WhenBankReturnsError_ReturnsFailureResult(HttpStatusCode statusCode, string responseContent)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        SetupHttpResponse(statusCode, responseContent);

        // Act
        var result = await _acquiringBankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Authorized, Is.False);
        Assert.That(result.AuthorizationCode, Is.Null);
    }

    [TestCase("")]
    [TestCase("{ invalid json }")]
    public async Task ProcessPaymentAsync_WhenBankReturnsInvalidContent_ReturnsFailureResult(string responseContent)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        // Act
        var result = await _acquiringBankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Authorized, Is.False);
        Assert.That(result.AuthorizationCode, Is.Null);
    }

    [Test]
    public async Task ProcessPaymentAsync_WhenHttpRequestThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _acquiringBankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Authorized, Is.False);
        Assert.That(result.AuthorizationCode, Is.Null);
    }

    [Test]
    public async Task ProcessPaymentAsync_WhenRequestTimeouts_ReturnsFailureResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act
        var result = await _acquiringBankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Authorized, Is.False);
        Assert.That(result.AuthorizationCode, Is.Null);
    }

    [Test]
    public async Task ProcessPaymentAsync_SendsCorrectRequestFormat()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new { authorized = true, authorization_code = "eeb61d69-79f9-49fe-9547-5ea385dc2c5b" };

        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(bankResponse))
            });

        // Act
        await _acquiringBankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(capturedRequest.RequestUri!.PathAndQuery, Is.EqualTo("/payments"));
        Assert.That(capturedRequest.Content!.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));

        var requestBody = await capturedRequest.Content.ReadAsStringAsync();
        var requestJson = JsonSerializer.Deserialize<JsonElement>(requestBody);

        Assert.That(requestJson.GetProperty("card_number").GetString(), Is.EqualTo(request.CardNumber));
        Assert.That(requestJson.GetProperty("expiry_date").GetString(), Is.EqualTo(request.ExpiryDate));
        Assert.That(requestJson.GetProperty("currency").GetString(), Is.EqualTo(request.Currency));
        Assert.That(requestJson.GetProperty("amount").GetInt32(), Is.EqualTo(request.Amount));
        Assert.That(requestJson.GetProperty("cvv").GetString(), Is.EqualTo(request.Cvv));
    }

    private static PaymentRequest CreateValidPaymentRequest()
    {
        return new PaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = $"12/{DateTime.Now.Year + 1}",
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);
    }
}
