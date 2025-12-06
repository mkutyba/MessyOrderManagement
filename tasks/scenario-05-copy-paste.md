# Scenario 5: Copy-Paste Code - DRY Principle

## Workshop Objective
Participants will learn to identify and eliminate duplicate code by applying the DRY (Don't Repeat Yourself) principle, extracting common patterns into reusable methods or base classes.

## Problem Location
**Files involved:**
- `MessyOrderManagement/Controllers/CustomerController.cs`
  - `GetAllCustomers()` (lines 20-37) contains duplicate error handling pattern
- `MessyOrderManagement/Controllers/ProductController.cs`
  - `GetAllProducts()` (lines 20-37) is nearly identical to `GetAllCustomers()`
  - Similar patterns in `CreateCustomer()` and `CreateProduct()` methods
  - Similar patterns in `UpdateCustomer()` and `UpdateProduct()` methods

## Code Smell Identified
**Duplicate Code / Copy-Paste Programming** - The `GetAllCustomers()` and `GetAllProducts()` methods contain nearly identical code patterns (error handling, logging, try-catch blocks), violating the DRY principle. Similar duplication exists in Create and Update methods. This makes maintenance harder and increases the risk of bugs when changes are made to one but not the other.

## Current Code
```csharp
// CustomerController.cs
[HttpGet]
public IActionResult GetAllCustomers()
{
    logger.LogDebug("Getting all customers");
    var customers = new List<Customer>();
    try
    {
        customers = db.Customers.ToList();
        logger.LogInformation("Retrieved {Count} customers", customers.Count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving customers");
        return StatusCode(500);
    }

    return Ok(customers);
}

// ProductController.cs
[HttpGet]
public IActionResult GetAllProducts()
{
    logger.LogDebug("Getting all products");
    var products = new List<Product>();
    try
    {
        products = db.Products.ToList();
        logger.LogInformation("Retrieved {Count} products", products.Count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving products");
        return StatusCode(500);
    }

    return Ok(products);
}
```

## AI Prompts to Use
Create a base controller class that contains common helper methods for error handling and entity retrieval. Extract the duplicate try-catch-logging pattern into a reusable method. Have controllers inherit from this base class.
Create a generic helper method GetAllEntities<T> in the base controller that takes a DbSet and entity type name as parameters, handles the try-catch pattern with proper logging, and returns IActionResult. Use this method to replace both GetAllCustomers and GetAllProducts.
Create a helper method ExecuteWithErrorHandling that wraps operations in try-catch blocks with consistent logging. Use this to reduce duplication in Create and Update methods across controllers.

## Expected Result
After refactoring, the code should look like:

**BaseController.cs** (new file):
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MessyOrderManagement.Data;

namespace MessyOrderManagement.Controllers;

public abstract class BaseController : ControllerBase
{
    protected readonly ILogger logger;
    protected readonly OrderDbContext db;

    protected BaseController(ILogger logger, OrderDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

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
            return StatusCode(500);
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
            return StatusCode(500);
        }
    }
}
```

**CustomerController.cs** (updated):
```csharp
[ApiController]
[Route("api/customer")]
public class CustomerController : BaseController
{
    public CustomerController(ILogger<CustomerController> logger, OrderDbContext db)
        : base(logger, db)
    {
    }

    [HttpGet]
    public IActionResult GetAllCustomers()
    {
        return GetAllEntities(db.Customers, "customers");
    }

    // ... other methods can use ExecuteWithErrorHandling for common patterns
}
```

**ProductController.cs** (updated):
```csharp
[ApiController]
[Route("api/product")]
public class ProductController : BaseController
{
    public ProductController(ILogger<ProductController> logger, OrderDbContext db)
        : base(logger, db)
    {
    }

    [HttpGet]
    public IActionResult GetAllProducts()
    {
        return GetAllEntities(db.Products, "products");
    }

    // ... other methods can use ExecuteWithErrorHandling for common patterns
}
```

**Note:** The base controller approach allows sharing common patterns across multiple controllers while maintaining separation of concerns. Each controller can still have its own specific logic while leveraging shared error handling and entity retrieval patterns.

## How to Verify Success
### Manual checks:
- [ ] BaseController class is created with common helper methods
- [ ] CustomerController and ProductController inherit from BaseController
- [ ] Duplicate code between GetAllCustomers and GetAllProducts is eliminated
- [ ] Common logic is extracted into reusable methods in the base class
- [ ] Both endpoints still work correctly at `/api/customer` and `/api/product`
- [ ] Error handling is consistent across controllers
- [ ] Logging patterns are consistent

### Automated tests:
The existing integration tests should continue to pass. Create a test to verify the generic method works:

```csharp
[Fact]
public async Task GetAllCustomers_ShouldReturnListOfCustomers()
{
    // Arrange
    var client = Factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/customer");

    // Assert
    response.EnsureSuccessStatusCode();
    var customers = await response.Content.ReadFromJsonAsync<List<Customer>>();
    Assert.NotNull(customers);
}

[Fact]
public async Task GetAllProducts_ShouldReturnListOfProducts()
{
    // Arrange
    var client = Factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/product");

    // Assert
    response.EnsureSuccessStatusCode();
    var products = await response.Content.ReadFromJsonAsync<List<Product>>();
    Assert.NotNull(products);
}
```

Run tests:
```bash
dotnet test
```

## Time Estimate
- **Workshop time**: 15-20 minutes
- **Difficulty**: Beginner to Intermediate
- **Key learning**: DRY principle, code reuse, base classes, inheritance, generic methods, reducing duplication across multiple classes
