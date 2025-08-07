using Moq;

using PaymentGateway.Api.Core;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Unit.Services;

[TestFixture]
public class PaymentsServiceTests
{
    private PaymentsService _paymentsService = null!;
    private Mock<IAcquiringBank> _mockBank = null!;
    private Mock<IPaymentsRepository> _mockRepository = null!;

    [SetUp]
    public void SetUp()
    {
        _mockBank = new Mock<IAcquiringBank>();
        _mockRepository = new Mock<IPaymentsRepository>();
        _paymentsService = new PaymentsService(_mockRepository.Object, _mockBank.Object);
    }

    [Test]
    public async Task CreatePaymentAsync_WhenBankAuthorizesPayment_ReturnsAuthorizedStatus()
    {
        // Arrange
        PostPaymentRequest paymentRequest = CreatePaymentRequest();

        _mockBank
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Authorized = true,
                AuthorizationCode = Guid.NewGuid().ToString()
            });

        // Act
        var paymentResponse = await _paymentsService.CreatePaymentAsync(paymentRequest);

        // Assert
        Assert.That(paymentResponse.ExpiryMonth, Is.EqualTo(paymentResponse.ExpiryMonth));
        Assert.That(paymentResponse.ExpiryYear, Is.EqualTo(paymentRequest.ExpiryYear));
        Assert.That(paymentResponse.Amount, Is.EqualTo(paymentRequest.Amount));
        Assert.That(paymentResponse.CardNumberLastFour, Is.EqualTo(4321));
        Assert.That(paymentResponse.Currency, Is.EqualTo(paymentRequest.Currency));
        Assert.That(paymentResponse.Status, Is.EqualTo(PaymentStatus.Authorized));
        _mockBank.Verify(bank => bank.ProcessPaymentAsync(It.IsAny<PaymentRequest>()), Times.Once);
        _mockRepository.Verify(repo => repo.Add(It.IsAny<PostPaymentResponse>()), Times.Once);
    }

    [Test]
    public async Task CreatePaymentAsync_WhenBankDoesNotAuthorizesPayment_ReturnsDeclinedStatus()
    {
        // Arrange
        PostPaymentRequest paymentRequest = CreatePaymentRequest();

        _mockBank
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Authorized = false,
                AuthorizationCode = Guid.NewGuid().ToString()
            });

        // Act
        var paymentResponse = await _paymentsService.CreatePaymentAsync(paymentRequest);

        // Assert
        Assert.That(paymentResponse.ExpiryMonth, Is.EqualTo(paymentResponse.ExpiryMonth));
        Assert.That(paymentResponse.ExpiryYear, Is.EqualTo(paymentRequest.ExpiryYear));
        Assert.That(paymentResponse.Amount, Is.EqualTo(paymentRequest.Amount));
        Assert.That(paymentResponse.CardNumberLastFour, Is.EqualTo(4321));
        Assert.That(paymentResponse.Currency, Is.EqualTo(paymentRequest.Currency));
        Assert.That(paymentResponse.Status, Is.EqualTo(PaymentStatus.Declined));
        _mockBank.Verify(bank => bank.ProcessPaymentAsync(It.IsAny<PaymentRequest>()), Times.Once);
        _mockRepository.Verify(repo => repo.Add(It.IsAny<PostPaymentResponse>()), Times.Once);
    }

    [Test]
    public async Task CreatePaymentAsync_WhenBankUnsuccessfullyProcessesPayment_ReturnsRejectedStatus()
    {
        // Arrange
        PostPaymentRequest paymentRequest = CreatePaymentRequest();

        _mockBank
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = false,
                Authorized = false,
                AuthorizationCode = Guid.NewGuid().ToString()
            });

        // Act
        var paymentResponse = await _paymentsService.CreatePaymentAsync(paymentRequest);

        // Assert
        Assert.That(paymentResponse.ExpiryMonth, Is.EqualTo(paymentResponse.ExpiryMonth));
        Assert.That(paymentResponse.ExpiryYear, Is.EqualTo(paymentRequest.ExpiryYear));
        Assert.That(paymentResponse.Amount, Is.EqualTo(paymentRequest.Amount));
        Assert.That(paymentResponse.CardNumberLastFour, Is.EqualTo(4321));
        Assert.That(paymentResponse.Currency, Is.EqualTo(paymentRequest.Currency));
        Assert.That(paymentResponse.Status, Is.EqualTo(PaymentStatus.Rejected));
        _mockBank.Verify(bank => bank.ProcessPaymentAsync(It.IsAny<PaymentRequest>()), Times.Once);
        _mockRepository.Verify(repo => repo.Add(It.IsAny<PostPaymentResponse>()), Times.Never);
    }

    [Test]
    public async Task GetPaymentAsync_WhenPaymentExists_ReturnsPayment()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var expectedPayment = new PostPaymentResponse
        {
            Id = paymentId,
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = 1234,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000
        };

        _mockRepository
            .Setup(x => x.Get(paymentId))
            .Returns(expectedPayment);

        // Act
        var paymentResponse = await _paymentsService.GetPaymentAsync(paymentId);

        // Assert
        Assert.That(paymentResponse, Is.Not.Null);
        Assert.That(paymentResponse!.Id, Is.EqualTo(paymentId));
        Assert.That(paymentResponse!.Status, Is.EqualTo(PaymentStatus.Authorized));

        _mockRepository.Verify(r => r.Get(paymentId), Times.Once);
    }

    [Test]
    public async Task GetPaymentAsync_WhenPaymentNotFound_ReturnsNull()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.Get(paymentId))
            .Returns(null as PostPaymentResponse);

        // Act
        var result = await _paymentsService.GetPaymentAsync(paymentId);

        // Assert
        Assert.That(result, Is.Null);

        _mockRepository.Verify(r => r.Get(paymentId), Times.Once);
    }

    private static PostPaymentRequest CreatePaymentRequest()
    {
        int expiryYear = 2025;
        int expiryMonth = 12;
        string cardNumber = "4321432143214321";
        string currency = "GBP";
        int amount = 1000;
        int cvv = 123;

        PostPaymentRequest postPaymentRequest = new()
        {
            CardNumber = cardNumber,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            Currency = currency,
            Amount = amount,
            Cvv = cvv
        };
        return postPaymentRequest;
    }
}
