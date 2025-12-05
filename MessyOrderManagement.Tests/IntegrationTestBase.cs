using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MessyOrderManagement.Data;
using MessyOrderManagement.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MessyOrderManagement.Tests;

/// <summary>
/// WebApplicationFactory for integration tests using an in-memory database.
/// </summary>
public class IntegrationTestBase : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<OrderDbContext>));
            services.RemoveAll(typeof(DbContextOptions<OrderDbContext>));
            services.RemoveAll(typeof(OrderDbContext));

            // Use a consistent database name per factory instance to ensure persistence across HTTP requests
            services.AddDbContext<OrderDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                db.Database.EnsureCreated();

                if (!db.Customers.Any())
                {
                    db.Customers.Add(new Customer
                    {
                        Name = "Test Customer",
                        Email = "test@example.com",
                        Phone = "123-456-7890",
                        CreatedDate = DateTime.Now
                    });
                }

                if (!db.Products.Any())
                {
                    db.Products.Add(new Product
                    {
                        Name = "Test Product",
                        Price = 10.00m,
                        Stock = 100,
                        Category = "Test",
                        IsActive = true,
                        LastUpdated = DateTime.Now
                    });
                }

                db.SaveChanges();
            }
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        base.ConfigureWebHost(builder);
    }
}