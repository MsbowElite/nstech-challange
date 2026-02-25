using FluentAssertions;
using NUnit.Framework;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Domain;

[TestFixture]
public class OrderStatusTests
{
    [Test]
    public void FromValue_WithValidValue_ShouldReturnCorrectStatus()
    {
        // Act & Assert
        OrderStatus.FromValue(0).Should().Be(OrderStatus.Draft);
        OrderStatus.FromValue(1).Should().Be(OrderStatus.Placed);
        OrderStatus.FromValue(2).Should().Be(OrderStatus.Confirmed);
        OrderStatus.FromValue(3).Should().Be(OrderStatus.Canceled);
    }

    [Test]
    public void FromValue_WithInvalidValue_ShouldThrowException()
    {
        // Act & Assert
        var action = () => OrderStatus.FromValue(99);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid order status value*");
    }

    [Test]
    public void FromName_WithValidName_ShouldReturnCorrectStatus()
    {
        // Act & Assert
        OrderStatus.FromName("Draft").Should().Be(OrderStatus.Draft);
        OrderStatus.FromName("Placed").Should().Be(OrderStatus.Placed);
        OrderStatus.FromName("Confirmed").Should().Be(OrderStatus.Confirmed);
        OrderStatus.FromName("Canceled").Should().Be(OrderStatus.Canceled);
    }

    [Test]
    public void FromName_WithInvalidName_ShouldThrowException()
    {
        // Act & Assert
        var action = () => OrderStatus.FromName("InvalidStatus");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid order status name*");
    }

    [Test]
    public void CanTransitionToConfirmed_WhenPlaced_ShouldReturnTrue()
    {
        // Act & Assert
        OrderStatus.Placed.CanTransitionToConfirmed().Should().BeTrue();
    }

    [Test]
    public void CanTransitionToConfirmed_WhenNotPlaced_ShouldReturnFalse()
    {
        // Act & Assert
        OrderStatus.Draft.CanTransitionToConfirmed().Should().BeFalse();
        OrderStatus.Confirmed.CanTransitionToConfirmed().Should().BeFalse();
        OrderStatus.Canceled.CanTransitionToConfirmed().Should().BeFalse();
    }

    [Test]
    public void CanTransitionToCanceled_WhenPlacedOrConfirmed_ShouldReturnTrue()
    {
        // Act & Assert
        OrderStatus.Placed.CanTransitionToCanceled().Should().BeTrue();
        OrderStatus.Confirmed.CanTransitionToCanceled().Should().BeTrue();
    }

    [Test]
    public void CanTransitionToCanceled_WhenDraftOrCanceled_ShouldReturnFalse()
    {
        // Act & Assert
        OrderStatus.Draft.CanTransitionToCanceled().Should().BeFalse();
        OrderStatus.Canceled.CanTransitionToCanceled().Should().BeFalse();
    }

    [Test]
    public void IsTerminal_WhenCanceled_ShouldReturnTrue()
    {
        // Act & Assert
        OrderStatus.Canceled.IsTerminal().Should().BeTrue();
    }

    [Test]
    public void IsTerminal_WhenNotCanceled_ShouldReturnFalse()
    {
        // Act & Assert
        OrderStatus.Draft.IsTerminal().Should().BeFalse();
        OrderStatus.Placed.IsTerminal().Should().BeFalse();
        OrderStatus.Confirmed.IsTerminal().Should().BeFalse();
    }

    [Test]
    public void GetAll_ShouldReturnAllStatuses()
    {
        // Act
        var allStatuses = OrderStatus.GetAll().ToList();

        // Assert
        allStatuses.Should().HaveCount(4);
        allStatuses.Should().Contain(new[] 
        { 
            OrderStatus.Draft, 
            OrderStatus.Placed, 
            OrderStatus.Confirmed, 
            OrderStatus.Canceled 
        });
    }

    [Test]
    public void Equals_WithSameStatus_ShouldReturnTrue()
    {
        // Act & Assert
        OrderStatus.Placed.Equals(OrderStatus.Placed).Should().BeTrue();
        (OrderStatus.Placed == OrderStatus.Placed).Should().BeTrue();
    }

    [Test]
    public void Equals_WithDifferentStatus_ShouldReturnFalse()
    {
        // Act & Assert
        OrderStatus.Placed.Equals(OrderStatus.Confirmed).Should().BeFalse();
        (OrderStatus.Placed == OrderStatus.Confirmed).Should().BeFalse();
    }

    [Test]
    public void CompareTo_ShouldCompareByValue()
    {
        // Act & Assert
        OrderStatus.Draft.CompareTo(OrderStatus.Placed).Should().BeLessThan(0);
        OrderStatus.Confirmed.CompareTo(OrderStatus.Placed).Should().BeGreaterThan(0);
        OrderStatus.Placed.CompareTo(OrderStatus.Placed).Should().Be(0);
    }

    [Test]
    public void ToString_ShouldReturnName()
    {
        // Act & Assert
        OrderStatus.Placed.ToString().Should().Be("Placed");
        OrderStatus.Confirmed.ToString().Should().Be("Confirmed");
    }
}
