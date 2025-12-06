using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MessyOrderManagement.Data;
using MessyOrderManagement.Models;
using MessyOrderManagement.Repositories;
using MessyOrderManagement.Constants;
using Xunit;

namespace MessyOrderManagement.Tests;

public class OrderRepositoryTests : IClassFixture<IntegrationTestBase>
{
    private readonly IntegrationTestBase _factory;

    public OrderRepositoryTests(IntegrationTestBase factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllOrders()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        // Create test orders
        var order1 = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = OrderConstants.StatusPending,
            Date = DateTime.Now,
            Total = 20.00m
        };

        var order2 = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 3,
            Price = 15.00m,
            Status = OrderConstants.StatusActive,
            Date = DateTime.Now,
            Total = 45.00m
        };

        db.Orders.Add(order1);
        db.Orders.Add(order2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var orders = await repository.GetAllAsync();

        // Assert
        Assert.NotNull(orders);
        Assert.True(orders.Count >= 2);
        Assert.Contains(orders, o => o.Id == order1.Id);
        Assert.Contains(orders, o => o.Id == order2.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnOrder()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = OrderConstants.StatusPending,
            Date = DateTime.Now,
            Total = 20.00m
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repository.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(order.CustomerId, result.CustomerId);
        Assert.Equal(order.ProductId, result.ProductId);
        Assert.Equal(order.Total, result.Total);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        // Act
        var result = await repository.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_ShouldCreateOrder()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = OrderConstants.StatusPending,
            Date = DateTime.Now,
            Total = 20.00m
        };

        // Act
        var result = await repository.AddAsync(order);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(order.CustomerId, result.CustomerId);
        Assert.Equal(order.Total, result.Total);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOrder()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = OrderConstants.StatusPending,
            Date = DateTime.Now,
            Total = 20.00m
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Modify the order
        order.Quantity = 5;
        order.Price = 15.00m;
        order.Total = 75.00m;
        order.Status = OrderConstants.StatusActive;

        // Act
        var result = await repository.UpdateAsync(order);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Quantity);
        Assert.Equal(15.00m, result.Price);
        Assert.Equal(75.00m, result.Total);
        Assert.Equal(OrderConstants.StatusActive, result.Status);

        // Verify it's persisted
        var updated = await repository.GetByIdAsync(order.Id);
        Assert.NotNull(updated);
        Assert.Equal(5, updated.Quantity);
        Assert.Equal(75.00m, updated.Total);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteOrder()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = new Order
        {
            CustomerId = 1,
            ProductId = 1,
            Quantity = 2,
            Price = 10.00m,
            Status = OrderConstants.StatusPending,
            Date = DateTime.Now,
            Total = 20.00m
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var orderId = order.Id;

        // Act
        await repository.DeleteAsync(orderId);

        // Assert
        var deleted = await repository.GetByIdAsync(orderId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldNotThrow()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        // Act & Assert - Should not throw
        await repository.DeleteAsync(99999);
    }

    [Fact]
    public async Task GetSalesReportDataAsync_ShouldLoadCustomerAndProduct()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        // Ensure we have a customer and product
        var customer = db.Customers.FirstOrDefault() ?? new Customer
        {
            Name = "Test Customer",
            Email = "test@example.com",
            CreatedDate = DateTime.Now
        };
        if (customer.Id == 0)
        {
            db.Customers.Add(customer);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var product = db.Products.FirstOrDefault() ?? new Product
        {
            Name = "Test Product",
            Price = 10.00m,
            Stock = 100,
            IsActive = true,
            LastUpdated = DateTime.Now
        };
        if (product.Id == 0)
        {
            db.Products.Add(product);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Create orders with non-pending status
        var activeOrder = new Order
        {
            CustomerId = customer.Id,
            ProductId = product.Id,
            Quantity = 2,
            Price = 10.00m,
            Status = OrderConstants.StatusActive,
            Date = DateTime.Now,
            Total = 20.00m
        };

        var completedOrder = new Order
        {
            CustomerId = customer.Id,
            ProductId = product.Id,
            Quantity = 3,
            Price = 15.00m,
            Status = OrderConstants.StatusCompleted,
            Date = DateTime.Now,
            Total = 45.00m
        };

        db.Orders.Add(activeOrder);
        db.Orders.Add(completedOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var orders = await repository.GetSalesReportDataAsync();

        // Assert
        Assert.NotNull(orders);
        Assert.True(orders.Count >= 2);
        
        // Verify that Customer and Product are loaded (eager loading worked)
        var orderWithData = orders.FirstOrDefault(o => o.Id == activeOrder.Id || o.Id == completedOrder.Id);
        Assert.NotNull(orderWithData);
        Assert.NotNull(orderWithData.Customer); // Navigation property should be loaded
        Assert.NotNull(orderWithData.Product); // Navigation property should be loaded
        Assert.Equal(customer.Id, orderWithData.Customer.Id);
        Assert.Equal(product.Id, orderWithData.Product.Id);
    }

    [Fact]
    public async Task GetSalesReportDataAsync_ShouldExcludePendingOrders()
    {
        // Arrange
        using var scope = _factory.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var customer = db.Customers.FirstOrDefault() ?? new Customer
        {
            Name = "Test Customer",
            Email = "test@example.com",
            CreatedDate = DateTime.Now
        };
        if (customer.Id == 0)
        {
            db.Customers.Add(customer);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var product = db.Products.FirstOrDefault() ?? new Product
        {
            Name = "Test Product",
            Price = 10.00m,
            Stock = 100,
            IsActive = true,
            LastUpdated = DateTime.Now
        };
        if (product.Id == 0)
        {
            db.Products.Add(product);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var pendingOrder = new Order
        {
            CustomerId = customer.Id,
            ProductId = product.Id,
            Quantity = 2,
            Price = 10.00m,
            Status = OrderConstants.StatusPending,
            Date = DateTime.Now,
            Total = 20.00m
        };

        var activeOrder = new Order
        {
            CustomerId = customer.Id,
            ProductId = product.Id,
            Quantity = 3,
            Price = 15.00m,
            Status = OrderConstants.StatusActive,
            Date = DateTime.Now,
            Total = 45.00m
        };

        db.Orders.Add(pendingOrder);
        db.Orders.Add(activeOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var orders = await repository.GetSalesReportDataAsync();

        // Assert
        Assert.NotNull(orders);
        Assert.DoesNotContain(orders, o => o.Id == pendingOrder.Id);
        Assert.Contains(orders, o => o.Id == activeOrder.Id);
    }
}
