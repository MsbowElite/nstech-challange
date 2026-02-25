using FluentAssertions;
using NUnit.Framework;
using OrderService.Application.DTOs;
using OrderService.Application.Mappers;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Application.Mappers;

[TestFixture]
public class OrderMapperTests
{
    [Test]
    public void MapToResponse_WithValidOrder_ShouldMapAllProperties()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var items = new List<OrderItem>
        {
            new OrderItem(productId, 100.00m, 2)
        };
        var order = new Order(customerId, "USD", items);

        // Act
        var response = OrderMapper.MapToResponse(order);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be(order.Id);
        response.CustomerId.Should().Be(customerId);
        response.Status.Should().Be("Placed");
        response.Currency.Should().Be("USD");
        response.Total.Should().Be(200.00m);
        response.CreatedAt.Should().Be(order.CreatedAt);
        response.Items.Should().HaveCount(1);
        response.Items.First().ProductId.Should().Be(productId);
        response.Items.First().UnitPrice.Should().Be(100.00m);
        response.Items.First().Quantity.Should().Be(2);
        response.Items.First().Subtotal.Should().Be(200.00m);
    }

    [Test]
    public void MapToResponse_WithConfirmedOrder_ShouldMapConfirmedStatus()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();

        // Act
        var response = OrderMapper.MapToResponse(order);

        // Assert
        response.Status.Should().Be("Confirmed");
    }

    [Test]
    public void MapToResponse_WithCanceledOrder_ShouldMapCanceledStatus()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        // Act
        var response = OrderMapper.MapToResponse(order);

        // Assert
        response.Status.Should().Be("Canceled");
    }

    [Test]
    public void MapToResponse_WithMultipleItems_ShouldMapAllItems()
    {
        // Arrange
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 50.00m, 2),
            new OrderItem(Guid.NewGuid(), 75.00m, 1),
            new OrderItem(Guid.NewGuid(), 100.00m, 3)
        };
        var order = new Order(Guid.NewGuid(), "EUR", items);

        // Act
        var response = OrderMapper.MapToResponse(order);

        // Assert
        response.Items.Should().HaveCount(3);
        response.Total.Should().Be(475.00m); // (50*2) + (75*1) + (100*3)
    }

    private Order CreateTestOrder()
    {
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 100.00m, 1)
        };
        return new Order(Guid.NewGuid(), "USD", items);
    }
}
