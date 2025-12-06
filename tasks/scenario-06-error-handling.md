# Scenario 6: Error Handling - From Generic Errors to Structured Error Responses

## Workshop Objective
Participants will learn to improve error handling by replacing generic error responses with structured error messages, adding proper exception handling for specific exception types, and creating consistent error response formats across the API.

## Problem Location
**Files involved:**
- `MessyOrderManagement/Controllers/BaseController.cs`
  - `GetAllEntities` method (lines 27-31): Returns `StatusCode(500)` without error message
  - `GetEntityById` method (lines 49-53): Returns `StatusCode(500)` without error message
  - `ExecuteWithErrorHandling` methods (lines 75-86, 88-100): Return generic `StatusCode(500)` without error details
- `MessyOrderManagement/Controllers/OrderController.cs`
  - `GetAllOrders` method (lines 48-52): Returns `StatusCode(500)` without error message
  - All error responses lack structured error information

## Code Smell Identified
**Generic Error Responses / Poor Error Communication** - While error handling exists and exceptions are logged, all error responses return generic `StatusCode(500)` without error messages or structured error information. This makes it difficult for API consumers to understand what went wrong. The code also doesn't distinguish between different types of exceptions (database errors, validation errors, etc.).

## Current Code
```csharp
// BaseController.cs
protected IActionResult GetAllEntities<T>(DbSet<T> dbSet, string entityTypeName) where T : class
{
    logger.LogDebug("Getting all {EntityType}", entityTypeName);
    try
    {
        var entities = dbSet.ToList();
        logger.LogInformation("Retrieved {Count} {EntityType}", entities.Count, entityTypeName);
        return Ok(entities);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving {EntityType}", entityTypeName);
        return StatusCode(500);  // ❌ Generic 500, no error message
    }
}

protected IActionResult ExecuteWithErrorHandling(Func<IActionResult> action, string operation, string entityType)
{
    try
    {
        return action();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error {Operation} {EntityType}", operation, entityType);
        return StatusCode(500);  // ❌ Generic 500, no error message
    }
}

// OrderController.cs
[HttpGet]
public IActionResult GetAllOrders()
{
    logger.LogInformation("Getting orders");
    var orders = new List<Order>();
    try
    {
        // ... query logic ...
        orders = query.ToList();
        logger.LogInformation("Found {Count} orders", orders.Count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving orders");
        return StatusCode(500);  // ❌ Generic 500, no error message
    }

    return Ok(orders);
}
```

## AI Prompts to Use
Create an ErrorResponse class in Models folder with Message and Details properties.
Then update BaseController error handling methods to return ErrorResponse instead of just StatusCode(500).
Make sure error messages are meaningful and only show exception details in development mode.
Also catch DbUpdateException specifically where it makes sense to give better error messages.

## Expected Result
After refactoring:

**ErrorResponse.cs** (new file):
```csharp
namespace MessyOrderManagement.Models;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}
```

**Updated BaseController.cs**:
```csharp
using Microsoft.EntityFrameworkCore;

public abstract class BaseController : ControllerBase
{
    // ... existing code ...

    protected IActionResult GetAllEntities<T>(DbSet<T> dbSet, string entityTypeName) where T : class
    {
        logger.LogDebug("Getting all {EntityType}", entityTypeName);
        try
        {
            var entities = dbSet.ToList();
            logger.LogInformation("Retrieved {Count} {EntityType}", entities.Count, entityTypeName);
            return Ok(entities);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error retrieving {EntityType}", entityTypeName);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = $"An error occurred while retrieving {entityTypeName}",
                Details = IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving {EntityType}", entityTypeName);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = $"An error occurred while retrieving {entityTypeName}",
                Details = IsDevelopment() ? ex.ToString() : null
            });
        }
    }

    protected IActionResult GetEntityById<T>(DbSet<T> dbSet, int id, string entityTypeName) where T : class
    {
        logger.LogDebug("Getting {EntityType} with ID: {EntityId}", entityTypeName, id);
        try
        {
            var entity = dbSet.Find(id);
            if (entity == null)
            {
                logger.LogWarning("{EntityType} {EntityId} not found", entityTypeName, id);
                return NotFound(new ErrorResponse 
                { 
                    Message = $"{entityTypeName} with ID {id} not found" 
                });
            }

            logger.LogInformation("{EntityType} {EntityId} retrieved successfully", entityTypeName, id);
            return Ok(entity);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error retrieving {EntityType} {EntityId}", entityTypeName, id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = $"An error occurred while retrieving {entityTypeName}",
                Details = IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving {EntityType} {EntityId}", entityTypeName, id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = $"An error occurred while retrieving {entityTypeName}",
                Details = IsDevelopment() ? ex.ToString() : null
            });
        }
    }

    protected IActionResult ExecuteWithErrorHandling(Func<IActionResult> action, string operation, string entityType)
    {
        try
        {
            return action();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error {Operation} {EntityType}", operation, entityType);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = $"An error occurred while {operation} {entityType}",
                Details = IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error {Operation} {EntityType}", operation, entityType);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = $"An error occurred while {operation} {entityType}",
                Details = IsDevelopment() ? ex.ToString() : null
            });
        }
    }

    private bool IsDevelopment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
}
```

**Updated OrderController.cs** (GetAllOrders method example):
```csharp
[HttpGet]
public IActionResult GetAllOrders()
{
    logger.LogInformation("Getting orders");
    var orders = new List<Order>();
    try
    {
        logger.LogDebug("Querying database for orders");
        var query = db.Orders.AsQueryable();
        
        if (Request.Query.ContainsKey("status"))
        {
            var status = Request.Query["status"].ToString();
            query = query.Where(o => o.Status == status);
            logger.LogDebug("Applied status filter: {Status}", status);
        }

        if (Request.Query.ContainsKey("customerId"))
        {
            if (!int.TryParse(Request.Query["customerId"].ToString(), out var custId))
            {
                logger.LogWarning("Invalid customerId parameter: {CustomerId}", Request.Query["customerId"]);
                return BadRequest(new ErrorResponse { Message = "Invalid customerId parameter" });
            }
            query = query.Where(o => o.CustomerId == custId);
            logger.LogDebug("Applied customer filter: {CustomerId}", custId);
        }

        orders = query.ToList();
        logger.LogInformation("Found {Count} orders", orders.Count);
    }
    catch (DbUpdateException ex)
    {
        logger.LogError(ex, "Database error retrieving orders");
        return StatusCode(500, new ErrorResponse 
        { 
            Message = "An error occurred while retrieving orders",
            Details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
                ? ex.Message 
                : null
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving orders");
        return StatusCode(500, new ErrorResponse 
        { 
            Message = "An error occurred while retrieving orders",
            Details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
                ? ex.ToString() 
                : null
        });
    }

    return Ok(orders);
}
```

**Note:** All controllers that inherit from `BaseController` will automatically benefit from the improved error handling. The `ExecuteWithErrorHandling` method is used throughout the controllers and will now return structured error responses.

## How to Verify Success
### Manual checks:
- [ ] ErrorResponse class is created with Message and Details properties
- [ ] BaseController error handling methods return ErrorResponse objects
- [ ] All error responses include meaningful messages
- [ ] Specific exception types (DbUpdateException) are caught where appropriate
- [ ] Exception details are only included in development mode
- [ ] HTTP status codes are appropriate (400 for bad requests, 404 for not found, 500 for server errors)
- [ ] All controllers benefit from improved error handling through BaseController

### Automated tests:
Create tests to verify error handling:

```csharp
[Fact]
public async Task GetAllOrders_WhenDatabaseError_ShouldReturn500WithErrorResponse()
{
    // Arrange - This would require mocking or using a test database
    // For integration test, you might need to simulate a database error
    var client = Factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/order");

    // Assert - In normal case should succeed
    // For error case, verify error response structure
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.NotNull(error.Message);
    }
}

[Fact]
public async Task DeleteOrder_WhenOrderNotFound_ShouldReturn404WithErrorResponse()
{
    // Arrange
    var client = Factory.CreateClient();
    var nonExistentId = 99999;

    // Act
    var response = await client.DeleteAsync($"/api/order/{nonExistentId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.NotNull(error);
    Assert.NotNull(error.Message);
    Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task GetAllCustomers_WhenErrorOccurs_ShouldReturn500WithErrorResponse()
{
    // Arrange
    var client = Factory.CreateClient();
    // Note: This test would need to simulate an error condition
    // In a real scenario, you might mock the database or use a test database

    // Act
    var response = await client.GetAsync("/api/customer");

    // Assert - In normal case should succeed
    // For error case, verify error response structure
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.NotNull(error.Message);
        Assert.NotEmpty(error.Message);
    }
}
```

Run existing tests to ensure nothing broke:
```bash
dotnet test
```

## Time Estimate
- **Workshop time**: 15-20 minutes
- **Difficulty**: Intermediate
- **Key learning**: Structured error responses, specific exception handling, development vs production error details, improving base class error handling, consistent API error format
