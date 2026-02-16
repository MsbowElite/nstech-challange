using FluentAssertions;
using OrderService.Domain.Entities;

namespace OrderService.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void CreateProduct_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var product = new Product("Laptop", 999.99m, 10);

        // Assert
        product.Should().NotBeNull();
        product.Name.Should().Be("Laptop");
        product.UnitPrice.Should().Be(999.99m);
        product.AvailableQuantity.Should().Be(10);
    }

    [Fact]
    public void ReserveStock_WithSufficientQuantity_ShouldSucceed()
    {
        // Arrange
        var product = new Product("Laptop", 999.99m, 10);

        // Act
        product.ReserveStock(5);

        // Assert
        product.AvailableQuantity.Should().Be(5);
    }

    [Fact]
    public void ReserveStock_WithInsufficientQuantity_ShouldThrowException()
    {
        // Arrange
        var product = new Product("Laptop", 999.99m, 10);

        // Act & Assert
        var action = () => product.ReserveStock(15);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public void ReleaseStock_ShouldIncreaseQuantity()
    {
        // Arrange
        var product = new Product("Laptop", 999.99m, 10);
        product.ReserveStock(5);

        // Act
        product.ReleaseStock(5);

        // Assert
        product.AvailableQuantity.Should().Be(10);
    }

    [Fact]
    public void UpdatePrice_WithValidPrice_ShouldSucceed()
    {
        // Arrange
        var product = new Product("Laptop", 999.99m, 10);

        // Act
        product.UpdatePrice(899.99m);

        // Assert
        product.UnitPrice.Should().Be(899.99m);
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdatePrice_WithNegativePrice_ShouldThrowException()
    {
        // Arrange
        var product = new Product("Laptop", 999.99m, 10);

        // Act & Assert
        var action = () => product.UpdatePrice(-100);
        action.Should().Throw<ArgumentException>();
    }
}
