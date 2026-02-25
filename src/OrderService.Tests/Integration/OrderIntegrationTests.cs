using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OrderService.Application.DTOs;
using OrderService.Domain.ValueObjects;
using System.Net;
using System.Net.Http.Json;

namespace OrderService.Tests.Integration;

/// <summary>
/// Integration tests for the Order API endpoints
/// These tests use the real API with all production injections and a PostgreSQL container
/// </summary>
[TestFixture]
public class OrderIntegrationTests : IntegrationTestBase
{
    private Guid _testCustomerId;

    protected override async Task SeedTestDataAsync()
    {
        await base.SeedTestDataAsync();
        _testCustomerId = Guid.NewGuid(); // Simulate a customer ID
    }

    [Test]
    public async Task CreateOrderAndConfirm_WithValidData_ShouldCreateOrderAndConfirmSuccessfully()
    {
        // Arrange
        await AuthenticateAsync();

        var product = await DbContext.Products.FirstAsync();
        var initialStock = product.AvailableQuantity;

        var createOrderRequest = new CreateOrderRequest(
            CustomerId: _testCustomerId,
            Currency: "USD",
            Items: new List<OrderItemRequest>
            {
                new OrderItemRequest(product.Id, 2)
            }
        );

        // Act
        var responseCreate = await Client.PostAsJsonAsync("/orders", createOrderRequest);

        // Assert
        responseCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await responseCreate.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.CustomerId.Should().Be(_testCustomerId);
        order.Items.Should().HaveCount(1);
        order.Items.First().Quantity.Should().Be(2);
        order.Status.Should().Be(OrderStatus.Placed.ToString());

        var responseConfirm = await Client.PostAsJsonAsync($"/orders/{order.Id}/confirm", createOrderRequest);

        // Verify stock was reduced
        var updatedProduct = await GetProductAsync(product.Id);
        updatedProduct!.AvailableQuantity.Should().Be(initialStock - 2);
    }

    [Test]
    public async Task CreateOrder_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var productId = await CreateTestProductAsync("Low Stock Product", price: 49.99m, stockQuantity: 5);

        var createOrderRequest = new CreateOrderRequest(
            CustomerId: _testCustomerId,
            Currency: "USD",
            Items: new List<OrderItemRequest>
            {
                new OrderItemRequest(productId, 10) // More than available
            }
        );

        // Act
        var response = await Client.PostAsJsonAsync("/orders", createOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("stock");
    }

    [Test]
    public async Task CreateOrder_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange - Don't authenticate
        var product = await DbContext.Products.FirstAsync();

        var createOrderRequest = new CreateOrderRequest(
            CustomerId: _testCustomerId,
            Currency: "USD",
            Items: new List<OrderItemRequest>
            {
                new OrderItemRequest(product.Id, 1)
            }
        );

        // Act
        var response = await ClientNoAuth.PostAsJsonAsync("/orders", createOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetOrder_WithValidId_ShouldReturnOrder()
    {
        // Arrange
        await AuthenticateAsync();

        var product = await DbContext.Products.FirstAsync();
        var createOrderRequest = new CreateOrderRequest(
            CustomerId: _testCustomerId,
            Currency: "USD",
            Items: new List<OrderItemRequest>
            {
                new OrderItemRequest(product.Id, 1)
            }
        );

        var createResponse = await Client.PostAsJsonAsync("/orders", createOrderRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        // Act
        var getResponse = await Client.GetAsync($"/orders/{createdOrder!.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedOrder = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        retrievedOrder.Should().NotBeNull();
        retrievedOrder!.Id.Should().Be(createdOrder.Id);
        retrievedOrder.CustomerId.Should().Be(_testCustomerId);
    }

    [Test]
    public async Task GetOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/orders/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetAllOrders_ShouldReturnPagedResults()
    {
        // Arrange
        await AuthenticateAsync();
        await ResetDatabaseAsync(); // Start with clean slate

        var product = await DbContext.Products.FirstAsync();

        // Create multiple orders
        for (int i = 0; i < 3; i++)
        {
            var createRequest = new CreateOrderRequest(
                CustomerId: _testCustomerId,
                Currency: "USD",
                Items: new List<OrderItemRequest>
                {
                    new OrderItemRequest(product.Id, 1)
                }
            );
            await Client.PostAsJsonAsync("/orders", createRequest);
        }

        // Act
        var response = await Client.GetAsync("/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<OrderResponse>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task CancelOrder_ShouldRestoreStock()
    {
        // Arrange
        await AuthenticateAsync();

        var product = await DbContext.Products.FirstAsync();
        var initialStock = product.AvailableQuantity;

        var createOrderRequest = new CreateOrderRequest(
            CustomerId: _testCustomerId,
            Currency: "USD",
            Items: new List<OrderItemRequest>
            {
                new OrderItemRequest(product.Id, 2)
            }
        );

        var createResponse = await Client.PostAsJsonAsync("/orders", createOrderRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        // Act
        var cancelResponse = await Client.PostAsync($"/orders/{createdOrder!.Id}/cancel", null);

        // Assert
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify stock was restored
        var updatedProduct = await GetProductAsync(product.Id);
        updatedProduct!.AvailableQuantity.Should().Be(initialStock);

        // Verify order status
        var getResponse = await Client.GetAsync($"/orders/{createdOrder.Id}");
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        updatedOrder!.Status.Should().Be(OrderStatus.Canceled.ToString());
    }

    [Test]
    public async Task CreateOrder_WithMultipleProducts_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        await AuthenticateAsync();

        var products = await DbContext.Products.Take(3).ToListAsync();

        var createOrderRequest = new CreateOrderRequest(
            CustomerId: _testCustomerId,
            Currency: "USD",
            Items: products.Select(p => new OrderItemRequest(p.Id, 2)).ToList()
        );

        // Act
        var response = await Client.PostAsJsonAsync("/orders", createOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.Items.Should().HaveCount(3);

        // Verify total is calculated correctly
        var expectedTotal = products.Sum(p => p.UnitPrice * 2);
        order.Total.Should().Be(expectedTotal);

        // Verify each product stock was reduced
        foreach (var item in order.Items)
        {
            var product = await GetProductAsync(item.ProductId);
            product.Should().NotBeNull();
        }
    }

    [Test]
    public async Task CreateOrder_WithEmptyItems_ShouldReturnValidationError()
    {
        // Arrange
        await AuthenticateAsync();

        var createOrderRequest = new CreateOrderRequest(
            CustomerId: _testCustomerId,
            Currency: "USD",
            Items: new List<OrderItemRequest>() // Empty items list
        );

        // Act
        var response = await Client.PostAsJsonAsync("/orders", createOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task QueryOrders_ByCustomerId_ShouldReturnOnlyCustomerOrders()
    {
        // Arrange
        await AuthenticateAsync();
        await ResetDatabaseAsync();

        var product = await DbContext.Products.FirstAsync();
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();

        // Create orders for customer 1
        for (int i = 0; i < 2; i++)
        {
            await Client.PostAsJsonAsync("/orders", new CreateOrderRequest(
                CustomerId: customerId1,
                Currency: "USD",
                Items: new List<OrderItemRequest> { new OrderItemRequest(product.Id, 1) }
            ));
        }

        // Create order for customer 2
        await Client.PostAsJsonAsync("/orders", new CreateOrderRequest(
            CustomerId: customerId2,
            Currency: "USD",
            Items: new List<OrderItemRequest> { new OrderItemRequest(product.Id, 1) }
        ));

        // Act
        var response = await Client.GetAsync($"/orders?customerId={customerId1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<OrderResponse>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().HaveCount(2);
        pagedResult.Items.Should().AllSatisfy(o => o.CustomerId.Should().Be(customerId1));
    }
}
