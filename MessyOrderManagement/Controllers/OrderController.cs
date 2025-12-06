using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MessyOrderManagement.Models;
using MessyOrderManagement.Data;
using MessyOrderManagement.Constants;
using MessyOrderManagement.Repositories;

namespace MessyOrderManagement.Controllers;

[ApiController]
[Route("api/order")]
public class OrderController : BaseController
{
    public OrderController(ILogger<OrderController> logger, OrderDbContext db, IOrderRepository orderRepository, IHostEnvironment environment)
        : base(logger, db, orderRepository, environment)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        logger.LogInformation("Getting orders");
        var orders = new List<Order>();
        try
        {
            logger.LogDebug("Querying database for orders");
            orders = await orderRepository.GetAllAsync();
            
            if (Request.Query.ContainsKey("status"))
            {
                var status = Request.Query["status"].ToString();
                orders = orders.Where(o => o.Status == status).ToList();
                logger.LogDebug("Applied status filter: {Status}", status);
            }

            if (Request.Query.ContainsKey("customerId"))
            {
                if (!int.TryParse(Request.Query["customerId"].ToString(), out var custId))
                {
                    logger.LogWarning("Invalid customerId parameter: {CustomerId}", Request.Query["customerId"]);
                    return BadRequest(new ErrorResponse { Message = "Invalid customerId parameter" });
                }
                orders = orders.Where(o => o.CustomerId == custId).ToList();
                logger.LogDebug("Applied customer filter: {CustomerId}", custId);
            }

            logger.LogInformation("Found {Count} orders", orders.Count);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error retrieving orders");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving orders",
                Details = environment.IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving orders",
                Details = environment.IsDevelopment() ? ex.ToString() : null
            });
        }

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        logger.LogDebug("Getting order with ID: {OrderId}", id);
        try
        {
            var order = await orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                logger.LogWarning("Order {OrderId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Message = $"order with ID {id} not found"
                });
            }

            logger.LogInformation("Order {OrderId} retrieved successfully", id);
            return Ok(order);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error retrieving order {OrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving order",
                Details = environment.IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving order",
                Details = environment.IsDevelopment() ? ex.ToString() : null
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order? order)
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

            var createdOrder = await orderRepository.AddAsync(order);
            logger.LogInformation("Order created successfully with ID: {OrderId}", createdOrder.Id);
            return CreatedAtAction(nameof(GetOrder), new
            {
                id = createdOrder.Id
            }, createdOrder);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error creating order");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while creating order",
                Details = environment.IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while creating order",
                Details = environment.IsDevelopment() ? ex.ToString() : null
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order? order)
    {
        if (order == null)
        {
            logger.LogWarning("UpdateOrder called with null order");
            return BadRequest();
        }

        logger.LogDebug("Updating order {OrderId}", id);
        try
        {
            var existing = await orderRepository.GetByIdAsync(id);
            if (existing == null)
            {
                logger.LogWarning("Order {OrderId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Message = $"order with ID {id} not found"
                });
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
            var updatedOrder = await orderRepository.UpdateAsync(existing);
            order.Id = id;
            logger.LogInformation("Order {OrderId} updated successfully", id);
            return Ok(order);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error updating order {OrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while updating order",
                Details = environment.IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating order {OrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while updating order",
                Details = environment.IsDevelopment() ? ex.ToString() : null
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        logger.LogInformation("Deleting order {OrderId}", id);
        try
        {
            var existing = await orderRepository.GetByIdAsync(id);
            if (existing == null)
            {
                logger.LogWarning("Order {OrderId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Message = $"order with ID {id} not found"
                });
            }

            await orderRepository.DeleteAsync(id);
            logger.LogInformation("Order {OrderId} deleted successfully", id);
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error deleting order {OrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while deleting order",
                Details = environment.IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting order {OrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while deleting order",
                Details = environment.IsDevelopment() ? ex.ToString() : null
            });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
    {
        logger.LogInformation("Updating order {OrderId} status to {Status}", id, status);
        if (string.IsNullOrEmpty(status))
        {
            logger.LogWarning("UpdateOrderStatus called with empty status for order {OrderId}", id);
            return BadRequest();
        }

        try
        {
            var order = await orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                logger.LogWarning("Order {OrderId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Message = $"order with ID {id} not found"
                });
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
            await orderRepository.UpdateAsync(order);
            logger.LogInformation("Order {OrderId} status updated successfully to {Status}", id, status);
            return Ok();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error updating order {OrderId} status", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while updating order status",
                Details = environment.IsDevelopment() ? ex.Message : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating order {OrderId} status", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while updating order status",
                Details = environment.IsDevelopment() ? ex.ToString() : null
            });
        }
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