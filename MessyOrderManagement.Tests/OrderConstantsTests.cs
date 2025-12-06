using MessyOrderManagement.Constants;
using Xunit;

namespace MessyOrderManagement.Tests;

public class OrderConstantsTests
{
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

    [Fact]
    public void OrderConstants_ShouldHaveDefaultValues()
    {
        // Assert
        Assert.Equal(1, OrderConstants.DefaultCustomerId);
        Assert.Equal(1, OrderConstants.DefaultProductId);
        Assert.Equal(1, OrderConstants.DefaultQuantity);
        Assert.Equal(1, OrderConstants.DefaultPrice);
        Assert.Equal(0, OrderConstants.ZeroValue);
    }
}
