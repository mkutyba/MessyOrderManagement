using System.Net;
using System.Text;
using System.Text.Json;
using MessyOrderManagement.Models;
using Xunit;

namespace MessyOrderManagement.Tests;

public class OrderControllerTests : IClassFixture<IntegrationTestBase>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestBase _factory;

    public OrderControllerTests(IntegrationTestBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllOrders_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/order", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>(TestContext.Current.CancellationToken);
        Assert.NotNull(orders);
    }

    [Fact]
    public async Task GetAllOrders_WithStatusFilter_ShouldReturnFilteredOrders()
    {
        // Arrange
        var status = "Pending";

        // Act
        var response = await _client.GetAsync($"/api/order?status={status}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>(TestContext.Current.CancellationToken);
        Assert.NotNull(orders);
        if (orders.Count > 0)
        {
            Assert.All(orders, o => Assert.Equal(status, o.Status));
        }
    }

    [Fact]
    public async Task GetAllOrders_WithCustomerIdFilter_ShouldReturnFilteredOrders()
    {
        // Arrange
        var customerId = 1;

        // Act
        var response = await _client.GetAsync($"/api/order?customerId={customerId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>(TestContext.Current.CancellationToken);
        Assert.NotNull(orders);
        if (orders.Count > 0)
        {
            Assert.All(orders, o => Assert.Equal(customerId, o.CustomerId));
        }
    }

    [Fact]
    public async Task GetAllOrders_WithInvalidCustomerId_ShouldReturnBadRequestWithErrorResponse()
    {
        // Arrange
        var invalidCustomerId = "invalid";

        // Act
        var response = await _client.GetAsync($"/api/order?customerId={invalidCustomerId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Message);
        Assert.Contains("customerId", errorResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllOrders_WithStatusAndCustomerIdFilter_ShouldReturnFilteredOrders()
    {
        // Arrange
        var status = "Pending";
        var customerId = 1;

        // Act
        var response = await _client.GetAsync($"/api/order?status={status}&customerId={customerId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>(TestContext.Current.CancellationToken);
        Assert.NotNull(orders);
        if (orders.Count > 0)
        {
            Assert.All(orders, o =>
            {
                Assert.Equal(status, o.Status);
                Assert.Equal(customerId, o.CustomerId);
            });
        }
    }

    [Fact]
    public async Task GetOrder_WithValidId_ShouldReturnOrder()
    {
        // Arrange - First create an order to get a valid ID
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.50m,
            Status = "Pending",
            Date = DateTime.Now,
            Notes = "Test order"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        var orderId = createdOrder!.Id;

        // Act
        var response = await _client.GetAsync($"/api/order/{orderId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        Assert.NotNull(order);
        Assert.Equal(orderId, order.Id);
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var response = await _client.GetAsync($"/api/order/{invalidId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Message);
        Assert.Contains("not found", errorResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 3,
            Price = 15.75m,
            Status = "Pending",
            Date = DateTime.Now,
            Notes = "Test order creation"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdOrder = await response.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        Assert.NotNull(createdOrder);
        Assert.True(createdOrder.Id > 0);
        Assert.Equal(newOrder.Quantity * newOrder.Price, createdOrder.Total);
    }

    [Fact]
    public async Task CreateOrder_WithNullOrder_ShouldReturnBadRequest()
    {
        // Act - Sending null content returns UnsupportedMediaType at framework level
        var response = await _client.PostAsync("/api/order", null, TestContext.Current.CancellationToken);

        // Assert - Framework returns UnsupportedMediaType for null content
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task CreateOrder_WithoutStatus_ShouldSetDefaultStatus()
    {
        // Arrange
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Date = DateTime.Now
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdOrder = await response.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        Assert.Equal("Pending", createdOrder!.Status);
    }

    [Fact]
    public async Task UpdateOrder_WithValidData_ShouldReturnOk()
    {
        // Arrange - Create an order first
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Pending",
            Date = DateTime.Now
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        var orderId = createdOrder!.Id;

        // Update the order
        var updatedOrder = new Order
        {
            CustomerId = 1,
            ProductId = 2,
            Quantity = 5,
            Price = 20.00m,
            Status = "Active",
            Date = DateTime.Now,
            Notes = "Updated order"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/order/{orderId}", updatedOrder, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal(5, result.Quantity);
        Assert.Equal(20.00m, result.Price);
        Assert.Equal(100.00m, result.Total);
    }

    [Fact]
    public async Task UpdateOrder_WithNullOrder_ShouldReturnBadRequest()
    {
        // Act - Sending null content returns UnsupportedMediaType at framework level
        var response = await _client.PutAsync("/api/order/1", null, TestContext.Current.CancellationToken);

        // Assert - Framework returns UnsupportedMediaType for null content
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task UpdateOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = 99999;
        var order = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Pending",
            Date = DateTime.Now
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/order/{invalidId}", order, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Message);
        Assert.Contains("not found", errorResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteOrder_WithValidId_ShouldReturnNoContent()
    {
        // Arrange - Create an order first
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Pending",
            Date = DateTime.Now
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        var orderId = createdOrder!.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/order/{orderId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/order/{orderId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var response = await _client.DeleteAsync($"/api/order/{invalidId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Message);
        Assert.Contains("not found", errorResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithValidTransition_ShouldReturnOk()
    {
        // Arrange - Create a pending order
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Pending",
            Date = DateTime.Now.AddDays(-1) // Set date in past to ensure activation succeeds
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        var orderId = createdOrder!.Id;

        // Act - Update status to Active
        var statusJson = JsonSerializer.Serialize("Active");
        var content = new StringContent(statusJson, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/api/order/{orderId}/status", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithEmptyStatus_ShouldReturnBadRequest()
    {
        // Arrange
        var orderId = 1;
        var statusJson = JsonSerializer.Serialize("");
        var content = new StringContent(statusJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/order/{orderId}/status", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = 99999;
        var statusJson = JsonSerializer.Serialize("Active");
        var content = new StringContent(statusJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/order/{invalidId}/status", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Message);
        Assert.Contains("not found", errorResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateOrderStatus_FromPendingToCompleted_ShouldReturnBadRequest()
    {
        // Arrange - Create a pending order
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Pending",
            Date = DateTime.Now
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        var orderId = createdOrder!.Id;

        // Act - Try to complete a pending order (should fail)
        var statusJson = JsonSerializer.Serialize("Completed");
        var content = new StringContent(statusJson, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/api/order/{orderId}/status", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_FromActiveToShipped_ShouldReturnOk()
    {
        // Arrange - Create and activate an order
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Pending",
            Date = DateTime.Now.AddDays(-1)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        var orderId = createdOrder!.Id;

        // First activate it
        var activeStatusJson = JsonSerializer.Serialize("Active");
        var activeContent = new StringContent(activeStatusJson, Encoding.UTF8, "application/json");
        var activateResponse = await _client.PutAsync($"/api/order/{orderId}/status", activeContent, TestContext.Current.CancellationToken);

        // Verify activation succeeded
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        // Verify the order is actually Active by fetching it
        var getOrderResponse = await _client.GetAsync($"/api/order/{orderId}", TestContext.Current.CancellationToken);
        var activeOrder = await getOrderResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        Assert.Equal("Active", activeOrder!.Status);

        // Act - Ship it
        var shippedStatusJson = JsonSerializer.Serialize("Shipped");
        var shippedContent = new StringContent(shippedStatusJson, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/api/order/{orderId}/status", shippedContent, TestContext.Current.CancellationToken);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Fail($"Expected OK but got {response.StatusCode}. Error: {errorContent}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_FromActiveToPending_ShouldReturnBadRequest()
    {
        // Arrange - Create and activate an order
        var newOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Pending",
            Date = DateTime.Now.AddDays(-1) // Set date in past to ensure activation succeeds
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        var orderId = createdOrder!.Id;

        // First activate it
        var activeStatusJson = JsonSerializer.Serialize("Active");
        var activeContent = new StringContent(activeStatusJson, Encoding.UTF8, "application/json");
        var activateResponse = await _client.PutAsync($"/api/order/{orderId}/status", activeContent, TestContext.Current.CancellationToken);

        // Verify activation succeeded
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        // Verify the order is actually Active by fetching it
        var getOrderResponse = await _client.GetAsync($"/api/order/{orderId}", TestContext.Current.CancellationToken);
        var activeOrder = await getOrderResponse.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
        Assert.Equal("Active", activeOrder!.Status);

        // Act - Try to revert to pending (should fail)
        var pendingStatusJson = JsonSerializer.Serialize("Pending");
        var pendingContent = new StringContent(pendingStatusJson, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/api/order/{orderId}/status", pendingContent, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}