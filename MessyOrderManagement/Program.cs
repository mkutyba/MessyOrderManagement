using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MessyOrderManagement.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.IncludeFields = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Configure global HTTP JSON options for HttpClient (used in tests)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.IncludeFields = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Configure EF Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

app.MapControllers();

app.Run();

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program
{
}