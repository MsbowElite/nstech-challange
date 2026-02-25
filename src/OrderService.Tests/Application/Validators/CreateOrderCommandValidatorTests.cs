using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Validators;

namespace OrderService.Tests.Application.Validators;

[TestFixture]
public class CreateOrderCommandValidatorTests
{
    private CreateOrderCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateOrderCommandValidator();
    }

    [Test]
    public void Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest(
            customerId,
            "USD",
            new List<OrderItemRequest>
            {
                new OrderItemRequest(Guid.NewGuid(), 2)
            }
        ));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WithEmptyCustomerId_ShouldFail()
    {
        // Arrange
        var command = new CreateOrderCommand(new CreateOrderRequest(
            Guid.Empty,
            "USD",
            new List<OrderItemRequest>
            {
                new OrderItemRequest(Guid.NewGuid(), 2)
            }
        ));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Request.CustomerId);
    }

    [Test]
    public void Validate_WithEmptyCurrency_ShouldFail()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest(
            customerId,
            "",
            new List<OrderItemRequest>
            {
                new OrderItemRequest(Guid.NewGuid(), 2)
            }
        ));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Request.Currency);
    }

    [Test]
    public void Validate_WithEmptyItems_ShouldFail()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest(
            customerId,
            "USD",
            new List<OrderItemRequest>()
        ));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Request.Items);
    }

    [Test]
    public void Validate_WithZeroQuantity_ShouldFail()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest(
            customerId,
            "USD",
            new List<OrderItemRequest>
            {
                new OrderItemRequest(Guid.NewGuid(), 0)
            }
        ));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Request.Items[0].Quantity");
    }

    [Test]
    public void Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest(
            customerId,
            "USD",
            new List<OrderItemRequest>
            {
                new OrderItemRequest(Guid.Empty, 2)
            }
        ));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Request.Items[0].ProductId");
    }
}
