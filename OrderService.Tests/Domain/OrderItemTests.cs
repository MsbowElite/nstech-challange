using FluentAssertions;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Domain;

public class OrderItemTests
{
    [Fact]
    public void CreateOrderItem_WithValidData_ShouldSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = 100.00m;
        var quantity = 3;

        // Act
        var orderItem = new OrderItem(productId, unitPrice, quantity);

        // Assert
        orderItem.ProductId.Should().Be(productId);
        orderItem.UnitPrice.Should().Be(unitPrice);
        orderItem.Quantity.Should().Be(quantity);
        orderItem.Subtotal.Should().Be(300.00m);
    }

    [Fact]
    public void CreateOrderItem_WithZeroQuantity_ShouldThrowException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        var action = () => new OrderItem(productId, 100.00m, 0);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void CreateOrderItem_WithNegativePrice_ShouldThrowException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        var action = () => new OrderItem(productId, -50.00m, 2);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void Subtotal_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var orderItem = new OrderItem(Guid.NewGuid(), 49.99m, 5);

        // Assert
        orderItem.Subtotal.Should().Be(249.95m);
    }
}
