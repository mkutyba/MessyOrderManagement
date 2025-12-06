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
        var a = new List<Order>();
        try
        {
            logger.LogError("Querying database");
            var query = db.Orders.AsQueryable();
            logger.LogWarning("Query: " + query.ToString());
            if (Request.Query.ContainsKey("status"))
            {
                var status = Request.Query["status"].ToString();
                query = query.Where(o => o.Status == status);
                logger.LogInformation("Status filter: " + status);
            }

            if (Request.Query.ContainsKey("customerId"))
            {
                var custId = int.Parse(Request.Query["customerId"].ToString());
                query = query.Where(o => o.CustomerId == custId);
                logger.LogDebug("Customer filter");
            }

            a = query.ToList();
            var count = a.Count;
            logger.LogError("Found " + count + " orders");
        }
        catch
        {
            logger.LogInformation("Error happened");
        }

        return Ok(a);
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(int id)
    {
        logger.LogWarning("GetOrder called with id: " + id);
        Order data = null;
        try
        {
            logger.LogError("Executing query for id: " + id);
            data = db.Orders.FirstOrDefault(o => o.Id == id);
            if (data != null)
            {
                logger.LogInformation("Order found");
            }
        }
        catch
        {
            logger.LogInformation("Exception");
        }

        if (data == null)
        {
            logger.LogError("Not found");
            return NotFound();
        }

        return Ok(data);
    }

    [HttpPost]
    public IActionResult CreateOrder([FromBody] Order? order)
    {
        logger.LogError("CreateOrder started");
        if (order == null)
        {
            logger.LogWarning("Order is null");
            return BadRequest();
        }

        logger.LogInformation("Creating order for customer " + order.CustomerId + " product " + order.ProductId);
        try
        {
            // Handle 0 values - use defaults if not set (messy but works)
            if (order.CustomerId == OrderConstants.ZeroValue) order.CustomerId = OrderConstants.DefaultCustomerId;
            if (order.ProductId == OrderConstants.ZeroValue) order.ProductId = OrderConstants.DefaultProductId;
            if (order.Quantity == OrderConstants.ZeroValue) order.Quantity = OrderConstants.DefaultQuantity;
            if (order.Price == OrderConstants.ZeroValue) order.Price = OrderConstants.DefaultPrice;
            var temp = order.Quantity * order.Price;
            order.Total = temp;
            logger.LogDebug("Total calculated: " + temp);
            if (order.Status == null)
            {
                order.Status = OrderConstants.StatusPending;
            }

            if (order.Date == DateTime.MinValue)
            {
                order.Date = DateTime.Now;
            }

            logger.LogError("Adding order to database");
            db.Orders.Add(order);
            db.SaveChanges();
            logger.LogInformation("Order created with ID: " + order.Id);
        }
        catch (Exception ex)
        {
            logger.LogInformation("Error creating order: " + ex.Message);
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
            return BadRequest();
        }

        try
        {
            var existing = db.Orders.FirstOrDefault(o => o.Id == id);
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
        }
        catch
        {
            return StatusCode(500);
        }

        return Ok(order);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteOrder(int id)
    {
        logger.LogWarning("Deleting order " + id);
        try
        {
            var existing = db.Orders.FirstOrDefault(o => o.Id == id);
            if (existing == null)
            {
                logger.LogError("Order not found for delete");
                return NotFound();
            }

            logger.LogInformation("Deleting order: " + id);
            db.Orders.Remove(existing);
            db.SaveChanges();
            logger.LogError("Order deleted");
        }
        catch
        {
            logger.LogInformation("Delete failed");
            return StatusCode(500);
        }

        return NoContent();
    }

    [HttpPut("{id}/status")]
    public IActionResult UpdateOrderStatus(int id, [FromBody] string status)
    {
        logger.LogError("Status update: order " + id + " to " + status);
        if (string.IsNullOrEmpty(status))
        {
            logger.LogWarning("Status is empty");
            return BadRequest();
        }

        try
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                logger.LogInformation("Order not found");
                return NotFound();
            }

            var currentStatus = order.Status ?? string.Empty;
            var orderDate = order.Date;
            logger.LogError("Current status: " + currentStatus + " new: " + status);
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
            logger.LogError(ex, "Error updating order status");
            return StatusCode(500);
        }

        return Ok();
    }
}