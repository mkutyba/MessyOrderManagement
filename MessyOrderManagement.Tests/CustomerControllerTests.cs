using System.Net;
using MessyOrderManagement.Models;
using Xunit;

namespace MessyOrderManagement.Tests;

public class CustomerControllerTests : IClassFixture<IntegrationTestBase>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestBase _factory;

    public CustomerControllerTests(IntegrationTestBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllCustomers_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/order/customer", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customers = await response.Content.ReadFromJsonAsync<List<Customer>>(TestContext.Current.CancellationToken);
        Assert.NotNull(customers);
    }

    [Fact]
    public async Task CreateCustomer_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var newCustomer = new Customer
        {
            Name = "Test Customer",
            Email = "test@example.com",
            Phone = "1234567890",
            Address = "123 Test St",
            City = "Test City",
            State = "TS",
            ZipCode = "12345",
            CreatedDate = DateTime.Now
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/order/customer", newCustomer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdCustomer = await response.Content.ReadFromJsonAsync<Customer>(TestContext.Current.CancellationToken);
        Assert.NotNull(createdCustomer);
        Assert.True(createdCustomer.Id > 0);
        Assert.Equal(newCustomer.Name, createdCustomer.Name);
        Assert.Equal(newCustomer.Email, createdCustomer.Email);
    }

    [Fact]
    public async Task CreateCustomer_WithNullCustomer_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.PostAsync("/api/order/customer", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCustomer_WithValidData_ShouldReturnOk()
    {
        // Arrange - Create a customer first
        var newCustomer = new Customer
        {
            Name = "Original Customer",
            Email = "original@example.com",
            Phone = "1234567890",
            Address = "123 Original St",
            City = "Original City",
            State = "OS",
            ZipCode = "12345",
            CreatedDate = DateTime.Now
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order/customer", newCustomer, TestContext.Current.CancellationToken);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<Customer>(TestContext.Current.CancellationToken);
        var customerId = createdCustomer!.Id;

        // Update the customer
        var updatedCustomer = new Customer
        {
            Name = "Updated Customer",
            Email = "updated@example.com",
            Phone = "9876543210",
            Address = "456 Updated Ave",
            City = "Updated City",
            State = "US",
            ZipCode = "54321",
            CreatedDate = DateTime.Now
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/order/customer/{customerId}", updatedCustomer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Customer>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(customerId, result.Id);
        Assert.Equal("Updated Customer", result.Name);
        Assert.Equal("updated@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateCustomer_WithNullCustomer_ShouldReturnBadRequest()
    {
        // Act - Sending null content returns UnsupportedMediaType at framework level
        var response = await _client.PutAsync("/api/order/customer/1", null, TestContext.Current.CancellationToken);

        // Assert - Framework returns UnsupportedMediaType for null content
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task UpdateCustomer_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = 99999;
        var customer = new Customer
        {
            Name = "Test Customer",
            Email = "test@example.com",
            Phone = "1234567890",
            Address = "123 Test St",
            City = "Test City",
            State = "TS",
            ZipCode = "12345",
            CreatedDate = DateTime.Now
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/order/customer/{invalidId}", customer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}