using FluentAssertions;
using NUnit.Framework;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Domain;

[TestFixture]
public class OrderDomainLogicTests
{
    [Test]
    public void Order_ShouldCalculateTotalCorrectly()
    {
        // Arrange & Act
        var order = new Order(Guid.NewGuid(), "USD", new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 100.00m, 2),
            new OrderItem(Guid.NewGuid(), 50.00m, 3),
            new OrderItem(Guid.NewGuid(), 25.00m, 4)
        });

        // Assert
        order.Total.Should().Be(450.00m); // (100*2) + (50*3) + (25*4)
    }

    [Test]
    public void Confirm_ShouldUpdateTimestamp()
    {
        // Arrange
        var order = CreateTestOrder();
        var originalCreatedAt = order.CreatedAt;

        // Act
        System.Threading.Thread.Sleep(10); // Ensure time difference
        order.Confirm();

        // Assert
        order.UpdatedAt.Should().NotBeNull();
        order.UpdatedAt.Should().BeAfter(originalCreatedAt);
    }

    [Test]
    public void Cancel_ShouldUpdateTimestamp()
    {
        // Arrange
        var order = CreateTestOrder();
        var originalCreatedAt = order.CreatedAt;

        // Act
        System.Threading.Thread.Sleep(10); // Ensure time difference
        order.Cancel();

        // Assert
        order.UpdatedAt.Should().NotBeNull();
        order.UpdatedAt.Should().BeAfter(originalCreatedAt);
    }

    [Test]
    public void Confirmable_ShouldThrowIfNotPlaced()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();

        // Act & Assert
        var action = () => order.Confirmable();
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Cancellable_ShouldSucceedWhenPlaced()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act & Assert
        var action = () => order.Cancellable();
        action.Should().NotThrow();
    }

    [Test]
    public void Cancellable_ShouldSucceedWhenConfirmed()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();

        // Act & Assert
        var action = () => order.Cancellable();
        action.Should().NotThrow();
    }

    [Test]
    public void Cancellable_ShouldThrowWhenAlreadyCanceled()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        // Act & Assert
        var action = () => order.Cancellable();
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void IsConfirmed_ShouldReturnTrueWhenConfirmed()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();

        // Act & Assert
        order.IsConfirmed().Should().BeTrue();
    }

    [Test]
    public void IsConfirmed_ShouldReturnFalseWhenNotConfirmed()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act & Assert
        order.IsConfirmed().Should().BeFalse();
    }

    [Test]
    public void CreateOrder_WithEmptyCustomerId_ShouldThrowException()
    {
        // Arrange & Act & Assert
        var action = () => new Order(Guid.Empty, "USD", new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 100.00m, 1)
        });

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Customer ID*");
    }

    [Test]
    public void CreateOrder_WithEmptyCurrency_ShouldThrowException()
    {
        // Arrange & Act & Assert
        var action = () => new Order(Guid.NewGuid(), "", new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 100.00m, 1)
        });

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Currency*");
    }

    [Test]
    public void CreateOrder_WithNullItems_ShouldThrowException()
    {
        // Arrange & Act & Assert
        var action = () => new Order(Guid.NewGuid(), "USD", null!);

        action.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Product_ShouldUpdateTimestampOnReserve()
    {
        // Arrange
        var product = new Product("Test Product", 100.00m, 10);
        var originalCreatedAt = product.CreatedAt;

        // Act
        System.Threading.Thread.Sleep(10);
        product.ReserveStock(5);

        // Assert
        product.UpdatedAt.Should().NotBeNull();
        product.UpdatedAt.Should().BeAfter(originalCreatedAt);
    }

    [Test]
    public void Product_ShouldUpdateTimestampOnRelease()
    {
        // Arrange
        var product = new Product("Test Product", 100.00m, 10);
        
        // Act
        System.Threading.Thread.Sleep(10);
        product.ReleaseStock(5);

        // Assert
        product.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void Product_CreateWithInvalidData_ShouldThrowExceptions()
    {
        // Empty name
        var action1 = () => new Product("", 100.00m, 10);
        action1.Should().Throw<ArgumentException>().WithMessage("*name*");

        // Negative price
        var action2 = () => new Product("Product", -10.00m, 10);
        action2.Should().Throw<ArgumentException>().WithMessage("*price*");

        // Negative quantity
        var action3 = () => new Product("Product", 100.00m, -5);
        action3.Should().Throw<ArgumentException>().WithMessage("*quantity*");
    }

    [Test]
    public void Product_ReserveStockWithZeroQuantity_ShouldThrowException()
    {
        // Arrange
        var product = new Product("Test Product", 100.00m, 10);

        // Act & Assert
        var action = () => product.ReserveStock(0);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    [Test]
    public void Product_ReleaseStockWithZeroQuantity_ShouldThrowException()
    {
        // Arrange
        var product = new Product("Test Product", 100.00m, 10);

        // Act & Assert
        var action = () => product.ReleaseStock(0);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    private Order CreateTestOrder()
    {
        return new Order(Guid.NewGuid(), "USD", new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 100.00m, 1)
        });
    }
}
