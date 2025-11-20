using System.Net;
using System.Net.Http.Json;
using MessyOrderManagement.Models;
using Xunit;

namespace MessyOrderManagement.Tests;

public class ProductControllerTests : IClassFixture<IntegrationTestBase>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestBase _factory;

    public ProductControllerTests(IntegrationTestBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/order/product", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>(TestContext.Current.CancellationToken);
        Assert.NotNull(products);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var newProduct = new Product
        {
            Name = "Test Product",
            Price = 29.99m,
            Stock = 100,
            Category = "Electronics",
            Description = "A test product",
            IsActive = true,
            LastUpdated = DateTime.Now
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/order/product", newProduct, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdProduct = await response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        Assert.NotNull(createdProduct);
        Assert.True(createdProduct.Id > 0);
        Assert.Equal(newProduct.Name, createdProduct.Name);
        Assert.Equal(newProduct.Price, createdProduct.Price);
    }

    [Fact]
    public async Task CreateProduct_WithNullProduct_ShouldReturnBadRequest()
    {
        // Act - Sending null content returns UnsupportedMediaType at framework level
        var response = await _client.PostAsync("/api/order/product", null, TestContext.Current.CancellationToken);

        // Assert - Framework returns UnsupportedMediaType for null content
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task CreateProduct_WithNullDescription_ShouldHandleGracefully()
    {
        // Arrange
        var newProduct = new Product
        {
            Name = "Test Product",
            Price = 15.99m,
            Stock = 25,
            Category = "Test",
            Description = null,
            IsActive = true,
            LastUpdated = DateTime.Now
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/order/product", newProduct, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdProduct = await response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        Assert.NotNull(createdProduct);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ShouldReturnOk()
    {
        // Arrange - Create a product first
        var newProduct = new Product
        {
            Name = "Original Product",
            Price = 10.00m,
            Stock = 50,
            Category = "Original",
            Description = "Original description",
            IsActive = true,
            LastUpdated = DateTime.Now
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order/product", newProduct, TestContext.Current.CancellationToken);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        var productId = createdProduct!.Id;

        // Update the product
        var updatedProduct = new Product
        {
            Name = "Updated Product",
            Price = 20.00m,
            Stock = 75,
            Category = "Updated",
            Description = "Updated description",
            IsActive = false,
            LastUpdated = DateTime.Now
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/order/product/{productId}", updatedProduct, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Updated Product", result.Name);
        Assert.Equal(20.00m, result.Price);
        Assert.Equal(75, result.Stock);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateProduct_WithNullProduct_ShouldReturnBadRequest()
    {
        // Act - Sending null content returns UnsupportedMediaType at framework level
        var response = await _client.PutAsync("/api/order/product/1", null, TestContext.Current.CancellationToken);

        // Assert - Framework returns UnsupportedMediaType for null content
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task UpdateProduct_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = 99999;
        var product = new Product
        {
            Name = "Test Product",
            Price = 15.99m,
            Stock = 25,
            Category = "Test",
            Description = "Test description",
            IsActive = true,
            LastUpdated = DateTime.Now
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/order/product/{invalidId}", product, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_ShouldUpdateLastUpdatedTimestamp()
    {
        // Arrange - Create a product first
        var newProduct = new Product
        {
            Name = "Test Product",
            Price = 10.00m,
            Stock = 50,
            Category = "Test",
            Description = "Test description",
            IsActive = true,
            LastUpdated = DateTime.Now.AddDays(-1)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/order/product", newProduct, TestContext.Current.CancellationToken);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        var productId = createdProduct!.Id;
        var originalLastUpdated = createdProduct.LastUpdated;

        // Wait a bit to ensure timestamp difference
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Update the product
        var updatedProduct = new Product
        {
            Name = "Updated Product",
            Price = 20.00m,
            Stock = 75,
            Category = "Updated",
            Description = "Updated description",
            IsActive = true,
            LastUpdated = DateTime.Now
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/order/product/{productId}", updatedProduct, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        Assert.True(Math.Abs((result!.LastUpdated - DateTime.Now).TotalSeconds) < 5);
    }
}