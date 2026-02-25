using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;
using OrderService.Application.Commands;
using OrderService.Application.Validators;

namespace OrderService.Tests.Application.Validators;

[TestFixture]
public class ConfirmOrderCommandValidatorTests
{
    private ConfirmOrderCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new ConfirmOrderCommandValidator();
    }

    [Test]
    public void Validate_WithValidOrderId_ShouldPass()
    {
        // Arrange
        var command = new ConfirmOrderCommand(Guid.NewGuid(), "test-key");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WithEmptyOrderId_ShouldFail()
    {
        // Arrange
        var command = new ConfirmOrderCommand(Guid.Empty, "test-key");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }
}
