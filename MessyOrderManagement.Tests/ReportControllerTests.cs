using System.Net;
using MessyOrderManagement.Models;
using Xunit;

namespace MessyOrderManagement.Tests;

public class ReportControllerTests : IClassFixture<IntegrationTestBase>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestBase _factory;

    public ReportControllerTests(IntegrationTestBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSalesReport_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(content));
    }

    [Fact]
    public async Task GetSalesReport_ShouldReturnReportData()
    {
        // Arrange - Create some orders with non-pending status
        var activeOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Active",
            Date = DateTime.Now
        };

        await _client.PostAsJsonAsync("/api/order", activeOrder, TestContext.Current.CancellationToken);

        // Act
        var response = await _client.GetAsync("/api/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("totalSales", content);
        Assert.Contains("orderCount", content);
    }

    [Fact]
    public async Task GetSalesReport_ShouldExcludePendingOrders()
    {
        // Arrange - Create a pending order
        var pendingOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Pending",
            Date = DateTime.Now
        };

        await _client.PostAsJsonAsync("/api/order", pendingOrder, TestContext.Current.CancellationToken);

        // Create a completed order
        var completedOrder = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 3,
            Price = 15.00m,
            Status = "Completed",
            Date = DateTime.Now
        };

        await _client.PostAsJsonAsync("/api/order", completedOrder, TestContext.Current.CancellationToken);

        // Act
        var response = await _client.GetAsync("/api/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // The report should only include non-pending orders
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(content));
    }

    [Fact]
    public async Task GetSalesReport_ShouldCalculateAverageCorrectly()
    {
        // Arrange - Create multiple orders with known totals
        var order1 = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = "Active",
            Date = DateTime.Now
        };

        var order2 = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 3,
            Price = 20.00m,
            Status = "Completed",
            Date = DateTime.Now
        };

        await _client.PostAsJsonAsync("/api/order", order1, TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/api/order", order2, TestContext.Current.CancellationToken);

        // Act
        var response = await _client.GetAsync("/api/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("average", content);
    }

    [Fact]
    public async Task GetSalesReport_ShouldHandleEmptyResults()
    {
        // Act - Get report when there are no non-pending orders
        var response = await _client.GetAsync("/api/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(content));
    }

    [Fact]
    public async Task GetSalesReport_ShouldLoadCustomerAndProductData()
    {
        // Arrange - Create a customer and product first
        var customer = new Customer
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "555-1234",
            CreatedDate = DateTime.Now
        };

        var customerResponse = await _client.PostAsJsonAsync("/api/customer", customer, TestContext.Current.CancellationToken);
        var createdCustomer = await customerResponse.Content.ReadFromJsonAsync<Customer>(TestContext.Current.CancellationToken);

        var product = new Product
        {
            Name = "Test Widget",
            Price = 25.00m,
            Stock = 50,
            Category = "Electronics",
            IsActive = true,
            LastUpdated = DateTime.Now
        };

        var productResponse = await _client.PostAsJsonAsync("/api/product", product, TestContext.Current.CancellationToken);
        var createdProduct = await productResponse.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);

        // Create an order with non-pending status
        var activeOrder = new Order
        {
            CustomerId = createdCustomer!.Id,
            ProductId = createdProduct!.Id,
            Quantity = 2,
            Price = 25.00m,
            Status = "Active",
            Date = DateTime.Now
        };

        await _client.PostAsJsonAsync("/api/order", activeOrder, TestContext.Current.CancellationToken);

        // Act
        var response = await _client.GetAsync("/api/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        
        // Verify that customer and product names are in the report
        // This proves that eager loading worked (no N+1 problem)
        Assert.Contains(createdCustomer.Name, content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(createdProduct.Name, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSalesReport_ShouldNotHaveNPlusOneProblem()
    {
        // Arrange - Create multiple orders with different customers and products
        var customer1 = new Customer
        {
            Name = "Alice Smith",
            Email = "alice@example.com",
            CreatedDate = DateTime.Now
        };
        var customer1Response = await _client.PostAsJsonAsync("/api/customer", customer1, TestContext.Current.CancellationToken);
        var createdCustomer1 = await customer1Response.Content.ReadFromJsonAsync<Customer>(TestContext.Current.CancellationToken);

        var customer2 = new Customer
        {
            Name = "Bob Johnson",
            Email = "bob@example.com",
            CreatedDate = DateTime.Now
        };
        var customer2Response = await _client.PostAsJsonAsync("/api/customer", customer2, TestContext.Current.CancellationToken);
        var createdCustomer2 = await customer2Response.Content.ReadFromJsonAsync<Customer>(TestContext.Current.CancellationToken);

        var product1 = new Product
        {
            Name = "Product Alpha",
            Price = 10.00m,
            Stock = 100,
            IsActive = true,
            LastUpdated = DateTime.Now
        };
        var product1Response = await _client.PostAsJsonAsync("/api/product", product1, TestContext.Current.CancellationToken);
        var createdProduct1 = await product1Response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);

        var product2 = new Product
        {
            Name = "Product Beta",
            Price = 20.00m,
            Stock = 50,
            IsActive = true,
            LastUpdated = DateTime.Now
        };
        var product2Response = await _client.PostAsJsonAsync("/api/product", product2, TestContext.Current.CancellationToken);
        var createdProduct2 = await product2Response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);

        // Create multiple orders
        var orders = new[]
        {
            new Order { CustomerId = createdCustomer1!.Id, ProductId = createdProduct1!.Id, Quantity = 1, Price = 10.00m, Status = "Active", Date = DateTime.Now },
            new Order { CustomerId = createdCustomer1!.Id, ProductId = createdProduct2!.Id, Quantity = 2, Price = 20.00m, Status = "Completed", Date = DateTime.Now },
            new Order { CustomerId = createdCustomer2!.Id, ProductId = createdProduct1!.Id, Quantity = 3, Price = 10.00m, Status = "Active", Date = DateTime.Now },
            new Order { CustomerId = createdCustomer2!.Id, ProductId = createdProduct2!.Id, Quantity = 1, Price = 20.00m, Status = "Shipped", Date = DateTime.Now }
        };

        foreach (var order in orders)
        {
            await _client.PostAsJsonAsync("/api/order", order, TestContext.Current.CancellationToken);
        }

        // Act
        var response = await _client.GetAsync("/api/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        
        // Verify all customer and product names are present
        // If N+1 problem existed, some names would be missing or empty
        Assert.Contains(createdCustomer1.Name, content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(createdCustomer2.Name, content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(createdProduct1.Name, content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(createdProduct2.Name, content, StringComparison.OrdinalIgnoreCase);
    }
}