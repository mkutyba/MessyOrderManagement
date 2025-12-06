using Microsoft.AspNetCore.Mvc;
using MessyOrderManagement.Models;
using MessyOrderManagement.Data;
using MessyOrderManagement.Constants;

namespace MessyOrderManagement.Controllers;

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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order for customer {CustomerId}", order.CustomerId);
            return StatusCode(500);
        }

        return CreatedAtAction(nameof(GetOrder), new
        {
            id = order.Id
        }, order);
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
        try
        {
            var existing = db.Orders.FirstOrDefault(o => o.Id == id);
            if (existing == null)
            {
                logger.LogWarning("Order {OrderId} not found for update", id);
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating order {OrderId}", id);
            return StatusCode(500);
        }

        return Ok(order);
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
}