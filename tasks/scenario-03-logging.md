# Scenario 8: Logging Anti-Patterns - From String Concatenation to Structured Logging

## Workshop Objective
Participants will learn to identify and fix logging anti-patterns including inappropriate log levels, string concatenation instead of structured logging, missing context, and inconsistent logging patterns. They will refactor to use proper structured logging with appropriate log levels and meaningful context.

## Problem Location
**Files involved:**
- `MessyOrderManagement/Controllers/OrderController.cs`
  - Inappropriate log levels: lines 28, 47, 64, 88, 116, 186, 200, 218
  - String concatenation: lines 30, 35, 47, 60, 64, 78, 95, 105, 119, 123, 173, 179, 183, 186, 200, 218
  - Logging errors as information: lines 51, 73, 123, 190
  - Missing exception context: lines 51, 73, 190
- `MessyOrderManagement/Controllers/CustomerController.cs`
  - String concatenation: lines 52, 53, 58, 73, 83, 103, 104
  - Logging errors as information: line 58
  - Logging stack traces in production: line 104
- `MessyOrderManagement/Controllers/ProductController.cs`
  - Missing logging entirely (no logging statements)
- `MessyOrderManagement/Controllers/ReportController.cs`
  - Inappropriate log levels: lines 22, 31, 51, 74, 79
  - String concatenation: lines 51, 60, 79
  - Logging errors as information: line 79

## Code Smell Identified
**Logging Anti-Patterns** - Multiple logging issues including:
1. **Inappropriate Log Levels**: Using `LogError` for informational messages, `LogInformation` for actual errors
2. **String Concatenation**: Using string concatenation (`+`) instead of structured logging with parameters
3. **Missing Context**: Log messages lack important context (IDs, counts, operation details)
4. **Inconsistent Patterns**: Different controllers use different logging styles
5. **Missing Logging**: Some controllers have no logging at all
6. **Exception Logging**: Logging exceptions as information instead of errors, or missing exception details

## Current Code

### OrderController.cs - Inappropriate Log Levels and String Concatenation
```csharp
[HttpGet]
public IActionResult GetAllOrders()
{
    logger.LogInformation("Getting orders");
    var a = new List<Order>();
    try
    {
        logger.LogError("Querying database");  // ❌ LogError for normal operation
        var query = db.Orders.AsQueryable();
        logger.LogWarning("Query: " + query.ToString());  // ❌ String concatenation + wrong level
        if (Request.Query.ContainsKey("status"))
        {
            var status = Request.Query["status"].ToString();
            query = query.Where(o => o.Status == status);
            logger.LogInformation("Status filter: " + status);  // ❌ String concatenation
        }
        // ...
        a = query.ToList();
        var count = a.Count;
        logger.LogError("Found " + count + " orders");  // ❌ LogError + string concatenation
    }
    catch
    {
        logger.LogInformation("Error happened");  // ❌ LogInformation for error, no exception details
    }
    return Ok(a);
}

[HttpGet("{id}")]
public IActionResult GetOrder(int id)
{
    logger.LogWarning("GetOrder called with id: " + id);  // ❌ String concatenation + wrong level
    Order data = null;
    try
    {
        logger.LogError("Executing query for id: " + id);  // ❌ LogError + string concatenation
        data = db.Orders.FirstOrDefault(o => o.Id == id);
        if (data != null)
        {
            logger.LogInformation("Order found");
        }
    }
    catch
    {
        logger.LogInformation("Exception");  // ❌ LogInformation for exception, no details
    }
    // ...
}

[HttpPost]
public IActionResult CreateOrder([FromBody] Order? order)
{
    logger.LogError("CreateOrder started");  // ❌ LogError for normal operation start
    // ...
    logger.LogInformation("Creating order for customer " + order.CustomerId + " product " + order.ProductId);  // ❌ String concatenation
    // ...
    logger.LogError("Adding order to database");  // ❌ LogError for normal operation
    // ...
    logger.LogInformation("Order created with ID: " + order.Id);  // ❌ String concatenation
    catch (Exception ex)
    {
        logger.LogInformation("Error creating order: " + ex.Message);  // ❌ LogInformation for error, missing exception object
        return StatusCode(500);
    }
}

[HttpDelete("{id}")]
public IActionResult DeleteOrder(int id)
{
    logger.LogWarning("Deleting order " + id);  // ❌ String concatenation
    try
    {
        // ...
        logger.LogError("Order not found for delete");  // ❌ LogError for not found (should be Warning)
        // ...
        logger.LogInformation("Deleting order: " + id);  // ❌ String concatenation
        // ...
        logger.LogError("Order deleted");  // ❌ LogError for success
    }
    catch
    {
        logger.LogInformation("Delete failed");  // ❌ LogInformation for error, no exception
        return StatusCode(500);
    }
}

[HttpPut("{id}/status")]
public IActionResult UpdateOrderStatus(int id, [FromBody] string status)
{
    logger.LogError("Status update: order " + id + " to " + status);  // ❌ LogError + string concatenation
    // ...
    logger.LogError("Current status: " + currentStatus + " new: " + status);  // ❌ LogError + string concatenation
    // ...
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating order status");  // ✅ This one is correct!
        return StatusCode(500);
    }
}
```

### CustomerController.cs - String Concatenation and Exception Logging Issues
```csharp
[HttpPost]
public IActionResult CreateCustomer([FromBody] Customer? customer)
{
    // ...
    logger.LogInformation("Customer created with ID: " + customer.Id);  // ❌ String concatenation
    logger.LogInformation("Returning created customer with ID: " + customer.Id);  // ❌ Redundant + string concatenation
    // ...
    catch (Exception ex)
    {
        logger.LogInformation("Error creating customer: " + ex.Message);  // ❌ LogInformation for error, missing exception object
        return StatusCode(500);
    }
}

[HttpPut("{id}")]
public IActionResult UpdateCustomer(int id, [FromBody] Customer customer)
{
    // ...
    logger.LogWarning($"Invalid customer ID {id} provided for update");  // ✅ Good use of interpolation
    // ...
    logger.LogWarning($"Customer with ID {id} not found for update. Total customers in DB: {allCustomers.Count}. Existing IDs: {string.Join(", ", allCustomers.Select(c => c.Id))}");  // ⚠️ Too verbose, potential performance issue
    // ...
    catch (Exception ex)
    {
        logger.LogError($"Error updating customer {id}: {ex.Message}");  // ⚠️ String interpolation instead of structured logging
        logger.LogError($"Stack trace: {ex.StackTrace}");  // ❌ Logging stack trace separately, should use exception object
        return StatusCode(500);
    }
}
```

### ProductController.cs - Missing Logging
```csharp
[HttpGet]
public IActionResult GetAllProducts()
{
    var a = new List<Product>();
    try
    {
        a = db.Products.ToList();
    }
    catch
    {
        // ❌ No logging at all
    }
    return Ok(a);
}

[HttpPost]
public IActionResult CreateProduct([FromBody] Product? product)
{
    // ❌ No logging for creation
    // ...
}

[HttpPut("{id}")]
public IActionResult UpdateProduct(int id, [FromBody] Product? product)
{
    // ❌ No logging for update
    // ...
}
```

### ReportController.cs - Inappropriate Log Levels
```csharp
[HttpGet("sales")]
public async Task<IActionResult> GetSalesReport()
{
    logger.LogError("Generating sales report");  // ❌ LogError for normal operation
    // ...
    logger.LogInformation("Sleeping...");  // ⚠️ Unnecessary logging
    // ...
    logger.LogWarning("Report query executed");  // ❌ LogWarning for normal operation
    // ...
    logger.LogError("Total sales: " + total + " count: " + count);  // ❌ LogError + string concatenation
    // ...
    logger.LogInformation("Writing to: " + filePath);  // ❌ String concatenation
    // ...
    logger.LogWarning("Report complete");  // ❌ LogWarning for success
    // ...
    catch
    {
        logger.LogInformation("Report error");  // ❌ LogInformation for error, no exception
        return StatusCode(500);
    }
}
```

## AI Prompts to Use
Refactor all logging statements in controllers to use appropriate log levels and structured logging.
Replace string concatenation with structured logging parameters.
Use LogInformation for normal operations, LogWarning for warnings (like not found), LogError only for actual errors, and LogDebug for detailed debugging.
When logging exceptions, always pass the exception object as the first parameter.
Include relevant context like order IDs, counts, and operation names in the log messages using structured logging format.

## Expected Result

After refactoring:

### OrderController.cs - Fixed Logging
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
                return BadRequest();
            }
            query = query.Where(o => o.CustomerId == custId);
            logger.LogDebug("Applied customer filter: {CustomerId}", custId);
        }

        orders = query.ToList();
        logger.LogInformation("Found {Count} orders", orders.Count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving orders");
        return StatusCode(500);
    }

    return Ok(orders);
}

[HttpGet("{id}")]
public IActionResult GetOrder(int id)
{
    logger.LogDebug("Getting order with ID: {OrderId}", id);
    Order order = null;
    try
    {
        order = db.Orders.FirstOrDefault(o => o.Id == id);
        if (order != null)
        {
            logger.LogInformation("Order {OrderId} retrieved successfully", id);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving order {OrderId}", id);
        return StatusCode(500);
    }

    if (order == null)
    {
        logger.LogWarning("Order {OrderId} not found", id);
        return NotFound();
    }

    return Ok(order);
}

[HttpPost]
public IActionResult CreateOrder([FromBody] Order? order)
{
    logger.LogInformation("Creating new order");
    if (order == null)
    {
        logger.LogWarning("CreateOrder called with null order");
        return BadRequest();
    }

    logger.LogDebug("Creating order for customer {CustomerId}, product {ProductId}, quantity {Quantity}", 
        order.CustomerId, order.ProductId, order.Quantity);
    
    try
    {
        // Handle 0 values - use defaults if not set
        if (order.CustomerId == 0) order.CustomerId = OrderConstants.DefaultCustomerId;
        if (order.ProductId == 0) order.ProductId = OrderConstants.DefaultProductId;
        if (order.Quantity == 0) order.Quantity = OrderConstants.DefaultQuantity;
        if (order.Price == 0) order.Price = OrderConstants.DefaultPrice;
        
        var total = order.Quantity * order.Price;
        order.Total = total;
        logger.LogDebug("Order total calculated: {Total}", total);
        
        if (order.Status == null)
        {
            order.Status = OrderConstants.StatusPending;
        }

        if (order.Date == DateTime.MinValue)
        {
            order.Date = DateTime.UtcNow;
        }

        db.Orders.Add(order);
        db.SaveChanges();
        logger.LogInformation("Order created successfully with ID: {OrderId}", order.Id);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating order for customer {CustomerId}", order.CustomerId);
        return StatusCode(500);
    }

    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}

[HttpDelete("{id}")]
public IActionResult DeleteOrder(int id)
{
    logger.LogInformation("Deleting order {OrderId}", id);
    try
    {
        var existing = db.Orders.FirstOrDefault(o => o.Id == id);
        if (existing == null)
        {
            logger.LogWarning("Order {OrderId} not found for deletion", id);
            return NotFound();
        }

        db.Orders.Remove(existing);
        db.SaveChanges();
        logger.LogInformation("Order {OrderId} deleted successfully", id);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting order {OrderId}", id);
        return StatusCode(500);
    }

    return NoContent();
}

[HttpPut("{id}/status")]
public IActionResult UpdateOrderStatus(int id, [FromBody] string status)
{
    logger.LogInformation("Updating order {OrderId} status to {Status}", id, status);
    if (string.IsNullOrEmpty(status))
    {
        logger.LogWarning("UpdateOrderStatus called with empty status for order {OrderId}", id);
        return BadRequest();
    }

    try
    {
        var order = db.Orders.FirstOrDefault(o => o.Id == id);
        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found for status update", id);
            return NotFound();
        }

        var currentStatus = order.Status ?? string.Empty;
        logger.LogDebug("Order {OrderId} status transition: {CurrentStatus} -> {NewStatus}", 
            id, currentStatus, status);
        
        // ... status transition logic ...
        
        logger.LogInformation("Order {OrderId} status updated successfully to {Status}", id, status);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating order {OrderId} status to {Status}", id, status);
        return StatusCode(500);
    }

    return Ok();
}
```

### CustomerController.cs - Fixed Logging
```csharp
[HttpPost]
public IActionResult CreateCustomer([FromBody] Customer? customer)
{
    if (customer == null)
    {
        logger.LogWarning("CreateCustomer called with null customer");
        return BadRequest();
    }

    try
    {
        if (customer.CreatedDate == DateTime.MinValue)
        {
            customer.CreatedDate = DateTime.UtcNow;
        }

        db.Customers.Add(customer);
        db.SaveChanges();
        logger.LogInformation("Customer created successfully with ID: {CustomerId}", customer.Id);
        return Created($"/api/customer/{customer.Id}", customer);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating customer");
        return StatusCode(500);
    }
}

[HttpPut("{id}")]
public IActionResult UpdateCustomer(int id, [FromBody] Customer customer)
{
    if (customer == null)
    {
        logger.LogWarning("UpdateCustomer called with null customer");
        return BadRequest();
    }

    if (id <= 0)
    {
        logger.LogWarning("Invalid customer ID {CustomerId} provided for update", id);
        return BadRequest();
    }

    try
    {
        var existing = db.Customers.FirstOrDefault(c => c.Id == id);
        if (existing == null)
        {
            logger.LogWarning("Customer {CustomerId} not found for update", id);
            return NotFound();
        }

        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.ZipCode = customer.ZipCode;
        db.SaveChanges();

        customer.Id = id;
        customer.CreatedDate = existing.CreatedDate;
        logger.LogInformation("Customer {CustomerId} updated successfully", id);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating customer {CustomerId}", id);
        return StatusCode(500);
    }

    return Ok(customer);
}
```

### ProductController.cs - Added Logging
```csharp
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

[HttpPost]
public IActionResult CreateProduct([FromBody] Product? product)
{
    if (product == null)
    {
        logger.LogWarning("CreateProduct called with null product");
        return BadRequest();
    }

    logger.LogDebug("Creating product: {ProductName}", product.Name);
    try
    {
        if (product.LastUpdated == DateTime.MinValue)
        {
            product.LastUpdated = DateTime.UtcNow;
        }

        db.Products.Add(product);
        db.SaveChanges();
        logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating product: {ProductName}", product.Name);
        return StatusCode(500);
    }

    return CreatedAtAction(nameof(GetAllProducts), product);
}

[HttpPut("{id}")]
public IActionResult UpdateProduct(int id, [FromBody] Product? product)
{
    if (product == null)
    {
        logger.LogWarning("UpdateProduct called with null product");
        return BadRequest();
    }

    logger.LogDebug("Updating product {ProductId}", id);
    try
    {
        var existing = db.Products.FirstOrDefault(p => p.Id == id);
        if (existing == null)
        {
            logger.LogWarning("Product {ProductId} not found for update", id);
            return NotFound();
        }

        product.LastUpdated = DateTime.UtcNow;
        existing.Name = product.Name;
        existing.Price = product.Price;
        existing.Stock = product.Stock;
        existing.Category = product.Category;
        existing.Description = product.Description;
        existing.IsActive = product.IsActive;
        existing.LastUpdated = product.LastUpdated;
        db.SaveChanges();
        product.Id = id;
        
        logger.LogInformation("Product {ProductId} updated successfully", id);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating product {ProductId}", id);
        return StatusCode(500);
    }

    return Ok(product);
}
```

### ReportController.cs - Fixed Logging
```csharp
[HttpGet("sales")]
public async Task<IActionResult> GetSalesReport()
{
    logger.LogInformation("Generating sales report");
    var data = new List<object>();
    try
    {
        await Task.Delay(500); // Replaced Thread.Sleep
        var orders = db.Orders
            .Where(o => o.Status != "Pending")
            .ToList();
        
        logger.LogDebug("Retrieved {Count} non-pending orders for report", orders.Count);
        
        var total = 0.0m;
        var count = 0;
        foreach (var order in orders)
        {
            var customer = db.Customers.FirstOrDefault(c => c.Id == order.CustomerId);
            var product = db.Products.FirstOrDefault(p => p.Id == order.ProductId);
            var x = new
            {
                OrderId = order.Id,
                Date = order.Date,
                Total = order.Total,
                Customer = customer?.Name ?? "",
                Product = product?.Name ?? ""
            };
            data.Add(x);
            total = total + order.Total;
            count = count + 1;
        }

        logger.LogInformation("Sales report calculated: Total sales {TotalSales}, Order count {OrderCount}", 
            total, count);
        
        var report = new
        {
            Orders = data,
            TotalSales = total,
            OrderCount = count,
            Average = count > 0 ? total / count : 0
        };
        
        var filePath = "C:\\Reports\\sales_report_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
        logger.LogDebug("Writing report to file: {FilePath}", filePath);
        
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using (var file = new StreamWriter(filePath))
        {
            file.WriteLine("Sales Report - " + DateTime.Now.ToString());
            file.WriteLine("Total Sales: " + total);
            file.WriteLine("Order Count: " + count);
            file.WriteLine("Average: " + (count > 0 ? total / count : 0));
        }
        
        await Task.Delay(300); // Replaced Thread.Sleep
        logger.LogInformation("Sales report generated successfully");
        return Ok(report);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating sales report");
        return StatusCode(500);
    }
}
```

## How to Verify Success

### Manual checks:
- [ ] No `LogError` calls for normal operations (only for actual errors)
- [ ] No string concatenation in logging (all use structured logging with `{Parameter}` syntax)
- [ ] All exceptions are logged with the exception object passed to `LogError(ex, ...)`
- [ ] Log levels are appropriate: `LogDebug` for detailed info, `LogInformation` for normal operations, `LogWarning` for warnings, `LogError` for errors
- [ ] All controllers have consistent logging patterns
- [ ] ProductController now has logging statements
- [ ] No redundant logging (like logging the same thing twice)
- [ ] No stack trace logging (exception object contains this)
- [ ] Log messages include relevant context (IDs, counts, operation names)

### Automated tests:

Create tests to verify logging behavior (using a test logger):

```csharp
// Add to OrderControllerTests.cs
[Fact]
public async Task GetAllOrders_ShouldLogInformation()
{
    // Arrange
    var client = Factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/order", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    // In a real scenario, you would verify logs using a test logger
    // For this workshop, verify the functionality still works
    var orders = await response.Content.ReadFromJsonAsync<List<Order>>(TestContext.Current.CancellationToken);
    Assert.NotNull(orders);
}

[Fact]
public async Task GetOrder_WithInvalidId_ShouldLogWarning()
{
    // Arrange
    var client = Factory.CreateClient();
    var invalidId = 99999;

    // Act
    var response = await client.GetAsync($"/api/order/{invalidId}", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    // Verify warning was logged (would require test logger in real scenario)
}

[Fact]
public async Task CreateOrder_WithValidData_ShouldLogSuccess()
{
    // Arrange
    var client = Factory.CreateClient();
    var newOrder = new Order
    {
        CustomerId = 1,
        ProductId = 1,
        Quantity = 2,
        Price = 10.00m,
        Status = "Pending",
        Date = DateTime.Now
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/order", newOrder, TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var createdOrder = await response.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken);
    Assert.NotNull(createdOrder);
    Assert.True(createdOrder.Id > 0);
    // Verify success was logged (would require test logger in real scenario)
}

[Fact]
public async Task GetAllProducts_ShouldHaveLogging()
{
    // Arrange
    var client = Factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/product", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var products = await response.Content.ReadFromJsonAsync<List<Product>>(TestContext.Current.CancellationToken);
    Assert.NotNull(products);
    // Verify logging was added (would require test logger in real scenario)
}
```

Run existing tests to ensure nothing broke:
```bash
dotnet test
```

### Verification Checklist:
1. **Search for anti-patterns**:
   ```bash
   # Search for string concatenation in logging
   grep -r "LogError.*+" MessyOrderManagement/Controllers/
   grep -r "LogInformation.*+" MessyOrderManagement/Controllers/
   grep -r "LogWarning.*+" MessyOrderManagement/Controllers/
   
   # Should return no results after refactoring
   ```

2. **Verify structured logging**:
   ```bash
   # Search for structured logging pattern
   grep -r "LogInformation.*{" MessyOrderManagement/Controllers/
   # Should find many results with {Parameter} syntax
   ```

3. **Check exception logging**:
   ```bash
   # Verify exceptions are passed to LogError
   grep -r "LogError.*ex," MessyOrderManagement/Controllers/
   # Should find exception logging with exception object
   ```

## Time Estimate
- **Workshop time**: 20-25 minutes
- **Difficulty**: Intermediate
- **Key learning**: Structured logging, appropriate log levels, exception logging best practices, consistent logging patterns across controllers

## Additional Notes

### Log Level Guidelines:
- **LogDebug**: Detailed information for debugging (query details, intermediate calculations)
- **LogInformation**: Normal application flow (operations started/completed, counts retrieved)
- **LogWarning**: Warning conditions (not found, invalid input, business rule violations)
- **LogError**: Error conditions (exceptions, failures, unexpected states)

### Structured Logging Benefits:
- Better performance (no string concatenation overhead)
- Better searchability in log aggregation tools
- Ability to filter by specific parameters
- Consistent format across the application

### Exception Logging Best Practices:
- Always pass the exception object: `logger.LogError(ex, "Message")`
- Include context in the message: `logger.LogError(ex, "Error creating order {OrderId}", id)`
- Don't log stack traces separately - the exception object contains this
- Use appropriate log levels for exceptions (usually LogError)
