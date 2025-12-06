# Scenario 2: Magic Numbers and Strings - Extract Constants

## Workshop Objective
Participants will learn to identify and extract magic numbers and magic strings into well-named constants, improving code maintainability and reducing the risk of typos and inconsistencies.

## Problem Location
**Files involved:**
- `MessyOrderManagement/Controllers/OrderController.cs`
  - Magic strings: "Pending", "Active", "Completed", "Shipped" (lines 107, 218-329)
  - Magic numbers: 30 days (line 223), 8-18 hours (lines 232, 234)
  - Hardcoded defaults: 1, 0 (lines 98-101)
- `MessyOrderManagement/Controllers/ReportController.cs`
  - Magic strings: "Pending" (line 29)

## Code Smell Identified
**Magic Numbers and Magic Strings** - Hardcoded literal values scattered throughout the code without explanation. These values represent business rules, status values, or configuration that should be centralized and named meaningfully.

## Current Code
```csharp
// Magic strings throughout UpdateOrderStatus method
if (status == "Active")
{
    if (currentStatus == "Pending")
    {
        var daysDiff = (DateTime.Now - orderDate).Days;
        if (daysDiff < 30)  // Magic number: 30 days
        {
            if (orderDate.Hour > 8)  // Magic number: 8 hours
            {
                if (orderDate.Hour < 18)  // Magic number: 18 hours
                {
                    order.Status = "Active";
                    // ...
                }
            }
        }
    }
}

// Magic strings in CreateOrder
if (order.Status == null)
{
    order.Status = "Pending";  // Magic string
}

// Magic strings in GetSalesReport
var orders = db.Orders
    .Where(o => o.Status != "Pending")  // Magic string
    .ToList();
```

## AI Prompts to Use
Create a new file OrderConstants with a static class containing all order status values. Also add constants for business rules.
Replace all hardcoded status strings in OrderController with consts. Update all string comparisons to use these constants.
Replace all hardcoded status strings in ReportController with consts.
Replace the magic numbers with consts respectively.

## Expected Result
**OrderConstants.cs** (new file):
```csharp
namespace MessyOrderManagement.Constants;

public static class OrderConstants
{
    // Order Status Values
    public const string StatusPending = "Pending";
    public const string StatusActive = "Active";
    public const string StatusCompleted = "Completed";
    public const string StatusShipped = "Shipped";

    // Business Rules
    public const int MaxDaysForActivation = 30;
    public const int BusinessHoursStart = 8;
    public const int BusinessHoursEnd = 18;
}
```

**Updated OrderController.cs**:
```csharp
using MessyOrderManagement.Constants;

// In CreateOrder method:
if (order.Status == null)
{
    order.Status = OrderConstants.StatusPending;
}

// In UpdateOrderStatus method:
if (status == OrderConstants.StatusActive)
{
    if (currentStatus == OrderConstants.StatusPending)
    {
        var daysDiff = (DateTime.Now - orderDate).Days;
        if (daysDiff < OrderConstants.MaxDaysForActivation)
        {
            if (orderDate.Hour > OrderConstants.BusinessHoursStart)
            {
                if (orderDate.Hour < OrderConstants.BusinessHoursEnd)
                {
                    order.Status = OrderConstants.StatusActive;
                    // ...
                }
            }
        }
    }
}
```

**Updated ReportController.cs**:
```csharp
using MessyOrderManagement.Constants;

// In GetSalesReport method:
var orders = db.Orders
    .Where(o => o.Status != OrderConstants.StatusPending)
    .ToList();
```

## How to Verify Success
### Manual checks:
- [ ] OrderConstants.cs file exists with all status constants
- [ ] All magic strings "Pending", "Active", "Completed", "Shipped" are replaced with constants in OrderController.cs
- [ ] Magic string "Pending" is replaced with constant in ReportController.cs
- [ ] Magic numbers 30, 8, 18 are replaced with named constants
- [ ] Code compiles without errors
- [ ] All status comparisons use constants instead of literals in both OrderController and ReportController

### Automated tests:
Create a simple unit test to verify constants are accessible:

```csharp
[Fact]
public void OrderConstants_ShouldHaveAllStatusValues()
{
    // Arrange & Act
    var pending = OrderConstants.StatusPending;
    var active = OrderConstants.StatusActive;
    var completed = OrderConstants.StatusCompleted;
    var shipped = OrderConstants.StatusShipped;

    // Assert
    Assert.Equal("Pending", pending);
    Assert.Equal("Active", active);
    Assert.Equal("Completed", completed);
    Assert.Equal("Shipped", shipped);
}

[Fact]
public void OrderConstants_ShouldHaveBusinessRuleValues()
{
    // Assert
    Assert.Equal(30, OrderConstants.MaxDaysForActivation);
    Assert.Equal(8, OrderConstants.BusinessHoursStart);
    Assert.Equal(18, OrderConstants.BusinessHoursEnd);
}
```

Run existing integration tests to ensure functionality is preserved:
```bash
dotnet test
```

## Time Estimate
- **Workshop time**: 10-15 minutes
- **Difficulty**: Beginner
- **Key learning**: Constants extraction, maintainability, reducing magic values
