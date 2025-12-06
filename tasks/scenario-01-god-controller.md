# Scenario 1: God Controller - Breaking Down the Monolith

## Workshop Objective
Participants will learn how to identify and refactor a "God Controller" anti-pattern by splitting a single massive controller into focused, single-responsibility controllers following the Single Responsibility Principle (SRP).

## Problem Location
**Files involved:**
- `MessyOrderManagement/Controllers/OrderController.cs` (lines 1-583)
    - Handles Orders (lines 22-196)
    - Handles Customers (lines 198-287)
    - Handles Products (lines 289-363)
    - Handles Reports (lines 519-582)

## Code Smell Identified
**God Object/God Controller** - A single controller class that handles too many responsibilities, violating the Single Responsibility Principle. The `OrderController` manages Orders, Customers, Products, and Reports, making it difficult to maintain, test, and understand.

## Current Code
```csharp
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    // ... 583 lines handling Orders, Customers, Products, and Reports
    
    [HttpGet("customer")]
    public IActionResult GetAllCustomers() { /* ... */ }
    
    [HttpPost("customer")]
    public IActionResult CreateCustomer([FromBody] Customer? customer) { /* ... */ }
    
    [HttpGet("product")]
    public IActionResult GetAllProducts() { /* ... */ }
    
    [HttpPost("product")]
    public IActionResult CreateProduct([FromBody] Product? product) { /* ... */ }
    
    [HttpGet("report/sales")]
    public async Task<IActionResult> GetSalesReport() { /* ... */ }
}
```

## AI Prompts to Use
Split the OrderController into separate controllers: OrderController should only handle order endpoints, create CustomerController for customer endpoints,
ProductController for product endpoints, and ReportController for report endpoints. Each controller should have its own route prefix.
Update the route attributes. Ensure all endpoints maintain the same functionality. Update the route attributes in tests. Update also @api-test.http and @README.md
Ensure each new controller properly receives OrderDbContext and ILogger through constructor injection, following the same pattern as the original controller.

## Expected Result
After refactoring, you should have:

**OrderController.cs** (focused on orders only):
```csharp
[ApiController]
[Route("api/order")]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> logger;
    private readonly OrderDbContext db;

    public OrderController(ILogger<OrderController> logger, OrderDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

    [HttpGet]
    public IActionResult GetAllOrders() { /* ... */ }
    
    [HttpGet("{id}")]
    public IActionResult GetOrder(int id) { /* ... */ }
    
    [HttpPost]
    public IActionResult CreateOrder([FromBody] Order? order) { /* ... */ }
    
    [HttpPut("{id}")]
    public IActionResult UpdateOrder(int id, [FromBody] Order? order) { /* ... */ }
    
    [HttpDelete("{id}")]
    public IActionResult DeleteOrder(int id) { /* ... */ }
    
    [HttpPut("{id}/status")]
    public IActionResult UpdateOrderStatus(int id, [FromBody] string status) { /* ... */ }
}
```

**CustomerController.cs** (new file):
```csharp
[ApiController]
[Route("api/customer")]
public class CustomerController : ControllerBase
{
    private readonly ILogger<CustomerController> logger;
    private readonly OrderDbContext db;

    public CustomerController(ILogger<CustomerController> logger, OrderDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

    [HttpGet]
    public IActionResult GetAllCustomers() { /* ... */ }
    
    [HttpPost]
    public IActionResult CreateCustomer([FromBody] Customer? customer) { /* ... */ }
    
    [HttpPut("{id}")]
    public IActionResult UpdateCustomer(int id, [FromBody] Customer customer) { /* ... */ }
}
```

**ProductController.cs** (new file):
```csharp
[ApiController]
[Route("api/product")]
public class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> logger;
    private readonly OrderDbContext db;

    public ProductController(ILogger<ProductController> logger, OrderDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

    [HttpGet]
    public IActionResult GetAllProducts() { /* ... */ }
    
    [HttpPost]
    public IActionResult CreateProduct([FromBody] Product? product) { /* ... */ }
    
    [HttpPut("{id}")]
    public IActionResult UpdateProduct(int id, [FromBody] Product? product) { /* ... */ }
}
```

**ReportController.cs** (new file):
```csharp
[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly ILogger<ReportController> logger;
    private readonly OrderDbContext db;

    public ReportController(ILogger<ReportController> logger, OrderDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport() { /* ... */ }
}
```

## How to Verify Success
### Manual checks:
- [ ] OrderController only contains order-related endpoints
- [ ] CustomerController exists with route `/api/customer`
- [ ] ProductController exists with route `/api/product`
- [ ] ReportController exists with route `/api/report`
- [ ] All endpoints are accessible at their new routes
- [ ] No functionality is lost (all endpoints still work)

### Automated tests:
The existing integration tests in `MessyOrderManagement.Tests` should continue to pass, but you may need to update the route paths:

```csharp
// Update test routes from:
// GET /api/order/customer → GET /api/customer
// GET /api/order/product → GET /api/product
// GET /api/order/report/sales → GET /api/report/sales
```

Run the test suite:
```bash
dotnet test
```

All tests should pass with updated routes.

## Time Estimate
- **Workshop time**: 15-20 minutes
- **Difficulty**: Beginner
- **Key learning**: Single Responsibility Principle, controller organization
