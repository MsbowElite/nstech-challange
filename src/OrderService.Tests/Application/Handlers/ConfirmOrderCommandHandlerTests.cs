using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using OrderService.Application.Commands;
using OrderService.Application.Commands.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Application.Handlers;

[TestFixture]
public class ConfirmOrderCommandHandlerTests
{
    private Mock<IOrderRepository> _orderRepositoryMock = null!;
    private Mock<IProductRepository> _productRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private ConfirmOrderCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new ConfirmOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Test]
    public async Task Handle_WithValidOrder_ShouldConfirmAndReserveStock()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var order = new Order(Guid.NewGuid(), "USD", new List<OrderItem>
        {
            new OrderItem(productId, 100.00m, 2)
        });

        var product = CreateProductWithId(productId, "Test Product", 100.00m, 10);

        var command = new ConfirmOrderCommand(orderId, "test-key");

        _orderRepositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepositoryMock
            .Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Confirmed");
        order.Status.Should().Be(OrderStatus.Confirmed);
        product.AvailableQuantity.Should().Be(8); // 10 - 2

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_WithNonExistentOrder_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new ConfirmOrderCommand(orderId, "test-key");

        _orderRepositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task Handle_WithAlreadyConfirmedOrder_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(Guid.NewGuid(), "USD", new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 100.00m, 1)
        });
        order.Confirm(); // Already confirmed

        var command = new ConfirmOrderCommand(orderId, "test-key");

        _orderRepositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_WithConcurrencyException_ShouldRetry()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var callCount = 0;
        _orderRepositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                // Return a fresh order instance on each call
                return new Order(Guid.NewGuid(), "USD", new List<OrderItem>
                {
                    new OrderItem(productId, 100.00m, 2)
                });
            });

        var product = CreateProductWithId(productId, "Test Product", 100.00m, 10);

        var command = new ConfirmOrderCommand(orderId, "test-key");

        _productRepositoryMock
            .Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new DbUpdateConcurrencyException("Concurrency conflict");
                }
                return 1;
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Confirmed");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private Product CreateProductWithId(Guid id, string name, decimal price, int quantity)
    {
        var product = new Product(name, price, quantity);
        // Use reflection to set the Id
        var idProperty = typeof(Product).GetProperty("Id");
        idProperty?.SetValue(product, id);
        return product;
    }
}
