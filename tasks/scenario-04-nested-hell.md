# Scenario 4: Nested Hell - Flattening Deep Conditionals

## Workshop Objective
Participants will learn to refactor deeply nested conditional statements (7+ levels deep) into readable, maintainable code using early returns, guard clauses, and extraction methods.

## Problem Location
**Files involved:**
- `MessyOrderManagement/Controllers/OrderController.cs` (lines 205-359)
  - `UpdateOrderStatus` method contains 7+ levels of nested if statements

## Code Smell Identified
**Deeply Nested Conditionals / Arrow Code** - The `UpdateOrderStatus` method has excessive nesting (7+ levels), making it extremely difficult to read, test, and maintain. This violates the "Flat is Better than Nested" principle.

## Current Code
```csharp
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
        var orderDate = order.Date;
        logger.LogDebug("Order {OrderId} status transition: {CurrentStatus} -> {NewStatus}", 
            id, currentStatus, status);
        if (status == OrderConstants.StatusActive)
        {
            if (currentStatus == OrderConstants.StatusPending)
            {
                var daysDiff = (DateTime.Now - orderDate).Days;
                if (daysDiff < OrderConstants.MaxDaysForActivation)
                {
                    if (daysDiff > 0)
                    {
                        order.Status = OrderConstants.StatusActive;
                        db.SaveChanges();
                    }
                    else
                    {
                        if (orderDate.Hour > OrderConstants.BusinessHoursStart)
                        {
                            if (orderDate.Hour < OrderConstants.BusinessHoursEnd)
                            {
                                order.Status = OrderConstants.StatusActive;
                                db.SaveChanges();
                            }
                            else
                            {
                                return BadRequest("Cannot activate after hours");
                            }
                        }
                        else
                        {
                            return BadRequest("Cannot activate before hours");
                        }
                    }
                }
                else
                {
                    return BadRequest("Order too old");
                }
            }
            else
            {
                if (currentStatus == OrderConstants.StatusCompleted)
                {
                    return BadRequest("Cannot reactivate completed order");
                }
                else
                {
                    if (currentStatus == OrderConstants.StatusShipped)
                    {
                        return BadRequest("Cannot change shipped order");
                    }
                    else
                    {
                        order.Status = OrderConstants.StatusActive;
                        db.SaveChanges();
                    }
                }
            }
        }
        else
        {
            if (status == OrderConstants.StatusCompleted)
            {
                if (currentStatus == OrderConstants.StatusActive)
                {
                    order.Status = OrderConstants.StatusCompleted;
                    db.SaveChanges();
                }
                else
                {
                    if (currentStatus == OrderConstants.StatusPending)
                    {
                        return BadRequest("Cannot complete pending order");
                    }
                    else
                    {
                        if (currentStatus == OrderConstants.StatusShipped)
                        {
                            order.Status = OrderConstants.StatusCompleted;
                            db.SaveChanges();
                        }
                        else
                        {
                            return BadRequest("Invalid status transition");
                        }
                    }
                }
            }
            else
            {
                if (status == OrderConstants.StatusShipped)
                {
                    if (currentStatus == OrderConstants.StatusActive)
                    {
                        order.Status = OrderConstants.StatusShipped;
                        db.Entry(order).Property("Status").IsModified = true;
                        db.SaveChanges();
                    }
                    else
                    {
                        return BadRequest("Can only ship active orders");
                    }
                }
                else
                {
                    if (status == OrderConstants.StatusPending)
                    {
                        if (currentStatus == OrderConstants.StatusActive)
                        {
                            return BadRequest("Cannot revert to pending");
                        }
                        else
                        {
                            order.Status = OrderConstants.StatusPending;
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        return BadRequest("Invalid status");
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating order {OrderId} status to {Status}", id, status);
        return StatusCode(500);
    }

    logger.LogInformation("Order {OrderId} status updated successfully to {Status}", id, status);
    return Ok();
}
```

## AI Prompts to Use
1. Extract the status transition validation logic from UpdateOrderStatus into a separate private method ValidateStatusTransition that takes currentStatus and newStatus as parameters and returns a tuple (bool isValid, string? errorMessage). Use early returns and guard clauses to flatten the nested conditionals.

2. Create a private method CanActivateOrder that validates if an order can be activated. This method should return (bool canActivate, string? errorMessage). Use early returns to eliminate nesting.

3. Refactor UpdateOrderStatus to use the extracted methods. Use early returns for error cases and flatten all nested conditionals. The method should be no more than 2-3 levels deep.

## Expected Result
After refactoring, the code should look like:

```csharp
[HttpPut("{id}/status")]
public IActionResult UpdateOrderStatus(int id, [FromBody] string status)
{
    logger.LogInformation("Updating order {OrderId} status to {Status}", id, status);
    if (string.IsNullOrEmpty(status))
    {
        logger.LogWarning("UpdateOrderStatus called with empty status for order {OrderId}", id);
        return BadRequest("Status cannot be empty");
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

        var (isValid, errorMessage) = ValidateStatusTransition(currentStatus, status);
        if (!isValid)
        {
            logger.LogWarning("Invalid status transition for order {OrderId}: {ErrorMessage}", id, errorMessage);
            return BadRequest(errorMessage);
        }

        if (status == OrderConstants.StatusActive)
        {
            var (canActivate, activationError) = CanActivateOrder(order);
            if (!canActivate)
            {
                logger.LogWarning("Cannot activate order {OrderId}: {ErrorMessage}", id, activationError);
                return BadRequest(activationError);
            }
        }

        order.Status = status;
        db.SaveChanges();
        logger.LogInformation("Order {OrderId} status updated successfully to {Status}", id, status);
        return Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating order {OrderId} status to {Status}", id, status);
        return StatusCode(500);
    }
}

private (bool isValid, string? errorMessage) ValidateStatusTransition(string? currentStatus, string newStatus)
{
    currentStatus = currentStatus ?? string.Empty;

    // Cannot reactivate completed orders
    if (newStatus == OrderConstants.StatusActive && currentStatus == OrderConstants.StatusCompleted)
    {
        return (false, "Cannot reactivate completed order");
    }

    // Cannot change shipped orders
    if (currentStatus == OrderConstants.StatusShipped && newStatus != OrderConstants.StatusCompleted)
    {
        return (false, "Cannot change shipped order");
    }

    // Cannot complete pending orders directly
    if (newStatus == OrderConstants.StatusCompleted && currentStatus == OrderConstants.StatusPending)
    {
        return (false, "Cannot complete pending order");
    }

    // Can only ship active orders
    if (newStatus == OrderConstants.StatusShipped && currentStatus != OrderConstants.StatusActive)
    {
        return (false, "Can only ship active orders");
    }

    // Cannot revert active to pending
    if (newStatus == OrderConstants.StatusPending && currentStatus == OrderConstants.StatusActive)
    {
        return (false, "Cannot revert to pending");
    }

    return (true, null);
}

private (bool canActivate, string? errorMessage) CanActivateOrder(Order order)
{
    if (order.Status != OrderConstants.StatusPending)
    {
        return (true, null); // Already validated in ValidateStatusTransition
    }

    var daysDiff = (DateTime.Now - order.Date).Days;

    // Check 30-day limit
    if (daysDiff >= OrderConstants.MaxDaysForActivation)
    {
        return (false, "Order too old");
    }

    // If order is from today, check business hours
    if (daysDiff == 0)
    {
        var hour = order.Date.Hour;
        if (hour < OrderConstants.BusinessHoursStart)
        {
            return (false, "Cannot activate before hours");
        }
        if (hour >= OrderConstants.BusinessHoursEnd)
        {
            return (false, "Cannot activate after hours");
        }
    }

    return (true, null);
}
```

**Note:** The extracted methods should be private methods within the `OrderController` class. The logging statements are preserved in the main method to maintain observability, while the extracted validation methods focus on business logic without logging concerns.
```

## How to Verify Success
### Manual checks:
- [ ] UpdateOrderStatus method has no more than 3 levels of nesting
- [ ] Validation logic is extracted into separate methods
- [ ] Early returns are used for error cases
- [ ] Code is more readable and easier to follow
- [ ] All status transition rules are preserved

### Automated tests:
The existing integration tests should continue to pass. Additionally, create unit tests for the extracted methods:

```csharp
[Fact]
public void ValidateStatusTransition_WhenActiveToCompleted_ShouldAllow()
{
    // Arrange
    var currentStatus = OrderConstants.StatusActive;
    var newStatus = OrderConstants.StatusCompleted;

    // Act
    var (isValid, errorMessage) = ValidateStatusTransition(currentStatus, newStatus);

    // Assert
    Assert.True(isValid);
    Assert.Null(errorMessage);
}

[Fact]
public void ValidateStatusTransition_WhenPendingToCompleted_ShouldReject()
{
    // Arrange
    var currentStatus = OrderConstants.StatusPending;
    var newStatus = OrderConstants.StatusCompleted;

    // Act
    var (isValid, errorMessage) = ValidateStatusTransition(currentStatus, newStatus);

    // Assert
    Assert.False(isValid);
    Assert.NotNull(errorMessage);
}

[Fact]
public void CanActivateOrder_WhenOrderOlderThan30Days_ShouldReject()
{
    // Arrange
    var order = new Order
    {
        Status = OrderConstants.StatusPending,
        Date = DateTime.Now.AddDays(-31)
    };

    // Act
    var (canActivate, errorMessage) = CanActivateOrder(order);

    // Assert
    Assert.False(canActivate);
    Assert.Contains("too old", errorMessage);
}
```

Run tests:
```bash
dotnet test
```

## Time Estimate
- **Workshop time**: 20-25 minutes
- **Difficulty**: Intermediate
- **Key learning**: Early returns, guard clauses, method extraction, reducing cognitive complexity
