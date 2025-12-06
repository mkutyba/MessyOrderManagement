using Microsoft.AspNetCore.Mvc;
using MessyOrderManagement.Models;
using MessyOrderManagement.Data;
using MessyOrderManagement.Constants;

namespace MessyOrderManagement.Controllers;

[ApiController]
[Route("api/order")]
public class OrderController : BaseController
{
    public OrderController(ILogger<OrderController> logger, OrderDbContext db)
        : base(logger, db)
    {
    }

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
        return GetEntityById(db.Orders, id, "order");
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
        return ExecuteWithErrorHandling(
            () =>
            {
                // Handle 0 values - use defaults if not set (messy but works)
                if (order.CustomerId == OrderConstants.ZeroValue) order.CustomerId = OrderConstants.DefaultCustomerId;
                if (order.ProductId == OrderConstants.ZeroValue) order.ProductId = OrderConstants.DefaultProductId;
                if (order.Quantity == OrderConstants.ZeroValue) order.Quantity = OrderConstants.DefaultQuantity;
                if (order.Price == OrderConstants.ZeroValue) order.Price = OrderConstants.DefaultPrice;
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
                return CreatedAtAction(nameof(GetOrder), new
                {
                    id = order.Id
                }, order);
            },
            "creating",
            $"order for customer {order.CustomerId}");
    }

    [HttpPut("{id}")]
    public IActionResult UpdateOrder(int id, [FromBody] Order? order)
    {
        if (order == null)
        {
            logger.LogWarning("UpdateOrder called with null order");
            return BadRequest();
        }

        logger.LogDebug("Updating order {OrderId}", id);
        return ExecuteWithErrorHandling(
            () =>
            {
                var existing = FindEntityById(db.Orders, id, "order");
                if (existing == null)
                {
                    return NotFound();
                }

                var temp = order.Quantity * order.Price;
                order.Total = temp;
                existing.CustomerId = order.CustomerId;
                existing.ProductId = order.ProductId;
                existing.Quantity = order.Quantity;
                existing.Price = order.Price;
                existing.Status = order.Status;
                existing.Date = order.Date;
                existing.Notes = order.Notes;
                existing.Total = order.Total;
                db.SaveChanges();
                order.Id = id;
                logger.LogInformation("Order {OrderId} updated successfully", id);
                return Ok(order);
            },
            "updating",
            $"order {id}");
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteOrder(int id)
    {
        logger.LogInformation("Deleting order {OrderId}", id);
        return ExecuteWithErrorHandling(
            () =>
            {
                var existing = FindEntityById(db.Orders, id, "order");
                if (existing == null)
                {
                    return NotFound();
                }

                db.Orders.Remove(existing);
                db.SaveChanges();
                logger.LogInformation("Order {OrderId} deleted successfully", id);
                return NoContent();
            },
            "deleting",
            $"order {id}");
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

        return ExecuteWithErrorHandling(
            () =>
            {
                var order = FindEntityById(db.Orders, id, "order");
                if (order == null)
                {
                    return NotFound();
                }

                var currentStatus = order.Status ?? string.Empty;
                var orderDate = order.Date;
                logger.LogDebug("Order {OrderId} status transition: {CurrentStatus} -> {NewStatus}", 
                    id, currentStatus, status);

                var (isValid, errorMessage) = ValidateStatusTransition(currentStatus, status, orderDate);
                if (!isValid)
                {
                    logger.LogWarning("Invalid status transition for order {OrderId}: {ErrorMessage}", id, errorMessage);
                    return BadRequest(errorMessage);
                }

                order.Status = status;
                if (status == OrderConstants.StatusShipped)
                {
                    db.Entry(order).Property("Status").IsModified = true;
                }

                db.SaveChanges();
                logger.LogInformation("Order {OrderId} status updated successfully to {Status}", id, status);
                return Ok();
            },
            "updating status for",
            $"order {id}");
    }

    private (bool isValid, string? errorMessage) ValidateStatusTransition(string currentStatus, string newStatus, DateTime orderDate)
    {
        // Validate status value
        if (newStatus != OrderConstants.StatusPending &&
            newStatus != OrderConstants.StatusActive &&
            newStatus != OrderConstants.StatusCompleted &&
            newStatus != OrderConstants.StatusShipped)
        {
            return (false, "Invalid status");
        }

        // Transition to Active
        if (newStatus == OrderConstants.StatusActive)
        {
            if (currentStatus == OrderConstants.StatusCompleted)
            {
                return (false, "Cannot reactivate completed order");
            }

            if (currentStatus == OrderConstants.StatusShipped)
            {
                return (false, "Cannot change shipped order");
            }

            if (currentStatus == OrderConstants.StatusPending)
            {
                var daysDiff = (DateTime.Now - orderDate).Days;
                if (daysDiff >= OrderConstants.MaxDaysForActivation)
                {
                    return (false, "Order too old");
                }

                if (daysDiff == 0)
                {
                    if (orderDate.Hour <= OrderConstants.BusinessHoursStart)
                    {
                        return (false, "Cannot activate before hours");
                    }

                    if (orderDate.Hour >= OrderConstants.BusinessHoursEnd)
                    {
                        return (false, "Cannot activate after hours");
                    }
                }
            }

            return (true, null);
        }

        // Transition to Completed
        if (newStatus == OrderConstants.StatusCompleted)
        {
            if (currentStatus == OrderConstants.StatusPending)
            {
                return (false, "Cannot complete pending order");
            }

            if (currentStatus == OrderConstants.StatusActive || currentStatus == OrderConstants.StatusShipped)
            {
                return (true, null);
            }

            return (false, "Invalid status transition");
        }

        // Transition to Shipped
        if (newStatus == OrderConstants.StatusShipped)
        {
            if (currentStatus != OrderConstants.StatusActive)
            {
                return (false, "Can only ship active orders");
            }

            return (true, null);
        }

        // Transition to Pending
        if (newStatus == OrderConstants.StatusPending)
        {
            if (currentStatus == OrderConstants.StatusActive)
            {
                return (false, "Cannot revert to pending");
            }

            return (true, null);
        }

        return (false, "Invalid status transition");
    }
}