# Scenario 7: Tight Coupling and N+1 Query Problem

## Workshop Objective
Participants will learn to introduce the Repository pattern to reduce tight coupling between controllers and data access, and fix the N+1 query problem in the GetSalesReport method.

## Problem Location
**Files involved:**
- `MessyOrderManagement/Controllers/BaseController.cs`
  - Direct `OrderDbContext` dependency (line 11): All controllers inherit this tight coupling
- `MessyOrderManagement/Controllers/OrderController.cs`
  - Direct DbContext usage throughout (tight coupling)
- `MessyOrderManagement/Controllers/ReportController.cs`
  - Direct DbContext usage (line 12): Tight coupling
  - N+1 query problem in `GetSalesReport()` (lines 36-39)
    - Line 28-30: Query all orders
    - Lines 38-39: Query customer and product for EACH order in a loop

## Code Smell Identified
**Tight Coupling** - Controllers directly depend on `OrderDbContext`, making them hard to test and tightly coupled to Entity Framework. This makes it difficult to mock data access for unit testing and to swap out the data access layer. **N+1 Query Problem** - The `GetSalesReport` method executes one query to get orders, then executes additional queries inside a loop for each order to fetch related customer and product data, resulting in 1 + N queries instead of a single optimized query.

## Current Code
```csharp
// BaseController.cs
public abstract class BaseController : ControllerBase
{
    protected readonly OrderDbContext db;  // ❌ Direct dependency on DbContext

    protected BaseController(ILogger logger, OrderDbContext db, IHostEnvironment environment)
    {
        this.db = db;  // ❌ Tight coupling - all controllers inherit this
    }
}

// ReportController.cs
[HttpGet("sales")]
public async Task<IActionResult> GetSalesReport()
{
    var orders = db.Orders
        .Where(o => o.Status != OrderConstants.StatusPending)
        .ToList();  // First query: gets all orders
    
    foreach (var order in orders)
    {
        // ❌ N+1 problem: queries database for each order
        var customer = db.Customers.FirstOrDefault(c => c.Id == order.CustomerId);
        var product = db.Products.FirstOrDefault(p => p.Id == order.ProductId);
        // ... use customer and product ...
    }
}
```

## AI Prompts to Use
Create an IOrderRepository interface in a Repositories folder with methods like GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync, and GetSalesReportDataAsync. The GetSalesReportDataAsync should use Include to load customer and product in one query. Then create OrderRepository that implements this interface and uses OrderDbContext. Update BaseController to take IOrderRepository instead of OrderDbContext, and update all controllers to use the repository. Also update ReportController GetSalesReport to use the repository method instead of querying in a loop. Don't forget to register the repository in Program.cs with AddScoped. Oh and you'll probably need to add navigation properties to the Order model for Customer and Product so Include works.

## Expected Result
After refactoring:

**IOrderRepository.cs** (new file):
```csharp
namespace MessyOrderManagement.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<Order> AddAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task DeleteAsync(int id);
    Task<List<Order>> GetSalesReportDataAsync();
}
```

**OrderRepository.cs** (new file):
```csharp
using Microsoft.EntityFrameworkCore;
using MessyOrderManagement.Data;
using MessyOrderManagement.Models;

namespace MessyOrderManagement.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders.ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task DeleteAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Order>> GetSalesReportDataAsync()
    {
        // Eagerly load related data to avoid N+1 problem
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Product)
            .Where(o => o.Status != OrderConstants.StatusPending)
            .ToListAsync();
    }
}
```

**Updated BaseController.cs**:
```csharp
using MessyOrderManagement.Repositories;

public abstract class BaseController : ControllerBase
{
    protected readonly ILogger logger;
    protected readonly IOrderRepository orderRepository;  // ✅ Repository instead of DbContext
    protected readonly IHostEnvironment environment;

    protected BaseController(ILogger logger, IOrderRepository orderRepository, IHostEnvironment environment)
    {
        this.logger = logger;
        this.orderRepository = orderRepository;
        this.environment = environment;
    }
    
    // ... existing helper methods updated to use repository ...
}
```

**Updated ReportController.cs**:
```csharp
using MessyOrderManagement.Repositories;

[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly ILogger<ReportController> logger;
    private readonly IOrderRepository orderRepository;  // ✅ Repository instead of DbContext

    public ReportController(ILogger<ReportController> logger, IOrderRepository orderRepository)
    {
        this.logger = logger;
        this.orderRepository = orderRepository;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport()
    {
        logger.LogInformation("Generating sales report");
        var data = new List<object>();
        try
        {
            await Task.Delay(500);
            // ✅ Single query with eager loading - no N+1 problem!
            var orders = await orderRepository.GetSalesReportDataAsync();
            
            logger.LogDebug("Retrieved {Count} non-pending orders for report", orders.Count);
            
            var total = 0.0m;
            var count = 0;
            foreach (var order in orders)
            {
                // ✅ No more queries in loop - data already loaded!
                var x = new
                {
                    OrderId = order.Id,
                    Date = order.Date,
                    Total = order.Total,
                    Customer = order.Customer?.Name ?? "",
                    Product = order.Product?.Name ?? ""
                };
                data.Add(x);
                total = total + order.Total;
                count = count + 1;
            }

            logger.LogInformation("Sales report calculated: Total sales {TotalSales}, Order count {OrderCount}", 
                total, count);
            
            // ... rest of report generation ...
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating sales report");
            return StatusCode(500, new ErrorResponse { Message = "Error generating report" });
        }
    }
}
```

**Updated Program.cs**:
```csharp
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
```

**Note**: For this to work, you'll need to add navigation properties to Order model:
```csharp
public class Order
{
    // ... existing properties ...
    
    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }
    
    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
}
```

## How to Verify Success
### Manual checks:
- [ ] IOrderRepository interface exists with all required methods
- [ ] OrderRepository implements the interface
- [ ] BaseController uses IOrderRepository instead of OrderDbContext
- [ ] All controllers that inherit from BaseController now use repository
- [ ] ReportController uses IOrderRepository instead of OrderDbContext
- [ ] Repository is registered in Program.cs with AddScoped
- [ ] GetSalesReport uses repository method with eager loading (Include) instead of queries in loop
- [ ] Navigation properties (Customer and Product) are added to Order model

### Automated tests:
Create tests to verify the repository pattern and N+1 fix:

```csharp
[Fact]
public async Task GetSalesReport_ShouldLoadDataInSingleQuery()
{
    // Arrange
    var client = Factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/report/sales");

    // Assert
    response.EnsureSuccessStatusCode();
    // Verify that the report contains customer and product names
    // (which would be null if N+1 problem wasn't fixed)
}

[Fact]
public async Task GetAllOrders_ShouldUseRepository()
{
    // Arrange
    var client = Factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/order");

    // Assert
    response.EnsureSuccessStatusCode();
    var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
    Assert.NotNull(orders);
}

[Fact]
public async Task GetSalesReport_ShouldNotHaveNPlusOneProblem()
{
    // Arrange
    var client = Factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/report/sales");

    // Assert
    response.EnsureSuccessStatusCode();
    // Verify that customer and product names are populated
    // (which would be null/empty if N+1 problem wasn't fixed)
}
```

Run all tests:
```bash
dotnet test
```

## Time Estimate
- **Workshop time**: 25-30 minutes
- **Difficulty**: Intermediate-Advanced
- **Key learning**: Repository pattern, dependency inversion, eager loading, N+1 query problem, navigation properties
