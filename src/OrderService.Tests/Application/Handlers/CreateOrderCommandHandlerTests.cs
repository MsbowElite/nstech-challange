using FluentAssertions;
using Moq;
using NUnit.Framework;
using OrderService.Application.Commands;
using OrderService.Application.Commands.Handlers;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Application.Handlers;

[TestFixture]
public class CreateOrderCommandHandlerTests
{
    private Mock<IOrderRepository> _orderRepositoryMock = null!;
    private Mock<IProductRepository> _productRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private CreateOrderCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Test]
    public async Task Handle_WithValidRequest_ShouldCreateOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProductWithId(productId, "Test Product", 100.00m, 10);
        
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest(
            customerId,
            "USD",
            new List<OrderItemRequest>
            {
                new OrderItemRequest(productId, 2)
            }
        ));

        _productRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(command.Request.CustomerId);
        result.Currency.Should().Be("USD");
        result.Total.Should().Be(200.00m);
        result.Items.Should().HaveCount(1);

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_WithMissingProduct_ShouldThrowException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest(
            customerId,
            "USD",
            new List<OrderItemRequest>
            {
                new OrderItemRequest(productId, 2)
            }
        ));

        _productRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>()); // No products found

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task Handle_WithInsufficientStock_ShouldThrowException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProductWithId(productId, "Test Product", 100.00m, 5); // Only 5 available
        
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest(
            customerId,
            "USD",
            new List<OrderItemRequest>
            {
                new OrderItemRequest(productId, 10) // Requesting 10
            }
        ));

        _productRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
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
