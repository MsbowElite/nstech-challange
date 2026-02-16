using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void CreateOrder_WithValidData_ShouldSucceed()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var currency = "USD";
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 100.00m, 2)
        };

        // Act
        var order = new Order(customerId, currency, items);

        // Assert
        order.Should().NotBeNull();
        order.CustomerId.Should().Be(customerId);
        order.Currency.Should().Be(currency);
        order.Status.Should().Be(OrderStatus.Placed);
        order.Total.Should().Be(200.00m);
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public void CreateOrder_WithoutItems_ShouldThrowException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var currency = "USD";
        var emptyItems = new List<OrderItem>();

        // Act & Assert
        var action = () => new Order(customerId, currency, emptyItems);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public void ConfirmOrder_WhenPlaced_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ConfirmOrder_WhenAlreadyConfirmed_ShouldThrowException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Confirm();

        // Act & Assert
        var action = () => order.Confirm();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot confirm*");
    }

    [Fact]
    public void CancelOrder_WhenPlaced_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Canceled);
    }

    [Fact]
    public void CancelOrder_WhenConfirmed_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Confirm();

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Canceled);
    }

    [Fact]
    public void CancelOrder_WhenAlreadyCanceled_ShouldThrowException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Cancel();

        // Act & Assert
        var action = () => order.Cancel();
        action.Should().Throw<InvalidOperationException>();
    }

    private Order CreateValidOrder()
    {
        var customerId = Guid.NewGuid();
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 50.00m, 3)
        };
        return new Order(customerId, "USD", items);
    }
}
