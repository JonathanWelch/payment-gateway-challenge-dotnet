using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Unit.Controllers;

[TestFixture]
public class PaymentsControllerTests
{
    private Mock<IPaymentsService> _mockPaymentsService = null!;
    private PaymentsController _paymentsController = null!;

    [SetUp]
    public void Setup()
    {
        _mockPaymentsService = new Mock<IPaymentsService>();
        _paymentsController = new PaymentsController(_mockPaymentsService.Object);
    }

    [Test]
    public async Task GetPaymentAsync_WhenPaymentDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _mockPaymentsService
            .Setup(x => x.GetPaymentAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PostPaymentResponse?)null);

        // Act
        var result = await _paymentsController.GetPaymentAsync(Guid.NewGuid());

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetPaymentAsync_WhenPaymentExist_ReturnsOk()
    {
        // Arrange
        Guid paymentId = Guid.NewGuid();

        PostPaymentResponse paymentResponse = new()
        {
            Id = paymentId,
            ExpiryYear = 2025,
            ExpiryMonth = 12,
            Amount = 1000,
            CardNumberLastFour = 1234,
            Currency = "GBP",
            Status = PaymentStatus.Authorized
        };

        _mockPaymentsService
            .Setup(x => x.GetPaymentAsync(paymentId))
            .ReturnsAsync(paymentResponse);

        // Act
        var result = await _paymentsController.GetPaymentAsync(paymentId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var actualPaymentResponse = okResult!.Value;
        Assert.That(actualPaymentResponse, Is.SameAs(paymentResponse));
    }
}
