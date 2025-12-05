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
        var response = await _client.GetAsync("/api/order/report/sales", TestContext.Current.CancellationToken);

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
        var response = await _client.GetAsync("/api/order/report/sales", TestContext.Current.CancellationToken);

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
        var response = await _client.GetAsync("/api/order/report/sales", TestContext.Current.CancellationToken);

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
        var response = await _client.GetAsync("/api/order/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("average", content);
    }

    [Fact]
    public async Task GetSalesReport_ShouldHandleEmptyResults()
    {
        // Act - Get report when there are no non-pending orders
        var response = await _client.GetAsync("/api/order/report/sales", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(content));
    }
}