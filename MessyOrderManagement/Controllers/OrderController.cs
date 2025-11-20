using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using MessyOrderManagement.Models;
using System.Text;
using MessyOrderManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace MessyOrderManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private static string connString = "Server=localhost,1434;Database=MessyOrderDB;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=true;";
    private ILogger<OrderController> logger;
    private OrderDbContext db;

    public OrderController(ILogger<OrderController> log, OrderDbContext context)
    {
        logger = log;
        db = context;
        logger.LogError("Controller initialized with connection: " + connString);
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
            if (order.CustomerId == 0) order.CustomerId = 1;
            if (order.ProductId == 0) order.ProductId = 1;
            if (order.Quantity == 0) order.Quantity = 1;
            if (order.Price == 0) order.Price = 1;
            var temp = order.Quantity * order.Price;
            order.Total = temp;
            logger.LogDebug("Total calculated: " + temp);
            if (order.Status == null)
            {
                order.Status = "Pending";
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

    [HttpGet("customer")]
    public IActionResult GetAllCustomers()
    {
        var a = new List<Customer>();
        try
        {
            a = db.Customers.ToList();
        }
        catch
        {
        }

        return Ok(a);
    }

    [HttpPost("customer")]
    public IActionResult CreateCustomer([FromBody] Customer? customer)
    {
        if (customer == null)
        {
            return BadRequest();
        }

        try
        {
            if (customer.CreatedDate == DateTime.MinValue)
            {
                customer.CreatedDate = DateTime.Now;
            }

            db.Customers.Add(customer);
            db.SaveChanges();
            logger.LogInformation("Customer created with ID: " + customer.Id);
            logger.LogInformation("Returning created customer with ID: " + customer.Id);
            return Created($"/api/order/customer/{customer.Id}", customer);
        }
        catch (Exception ex)
        {
            logger.LogInformation("Error creating customer: " + ex.Message);
            return StatusCode(500);
        }
    }

    [HttpPut("customer/{id}")]
    public IActionResult UpdateCustomer(int id, [FromBody] Customer customer)
    {
        if (customer == null)
        {
            return BadRequest();
        }

        try
        {
            var existing = db.Customers.FirstOrDefault(c => c.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = customer.Name;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.ZipCode = customer.ZipCode;
            existing.CreatedDate = customer.CreatedDate;
            db.SaveChanges();
            customer.Id = id;
        }
        catch
        {
            return StatusCode(500);
        }

        return Ok(customer);
    }

    [HttpGet("product")]
    public IActionResult GetAllProducts()
    {
        var a = new List<Product>();
        try
        {
            a = db.Products.ToList();
        }
        catch
        {
        }

        return Ok(a);
    }

    [HttpPost("product")]
    public IActionResult CreateProduct([FromBody] Product? product)
    {
        if (product == null)
        {
            return BadRequest();
        }

        try
        {
            if (product.LastUpdated == DateTime.MinValue)
            {
                product.LastUpdated = DateTime.Now;
            }

            db.Products.Add(product);
            db.SaveChanges();
        }
        catch
        {
            return StatusCode(500);
        }

        return CreatedAtAction(nameof(GetAllProducts), product);
    }

    [HttpPut("product/{id}")]
    public IActionResult UpdateProduct(int id, [FromBody] Product? product)
    {
        if (product == null)
        {
            return BadRequest();
        }

        try
        {
            var existing = db.Products.FirstOrDefault(p => p.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            product.LastUpdated = DateTime.Now;
            existing.Name = product.Name;
            existing.Price = product.Price;
            existing.Stock = product.Stock;
            existing.Category = product.Category;
            existing.Description = product.Description;
            existing.IsActive = product.IsActive;
            existing.LastUpdated = product.LastUpdated;
            db.SaveChanges();
            product.Id = id;
        }
        catch
        {
            return StatusCode(500);
        }

        return Ok(product);
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
            if (status == "Active")
            {
                if (currentStatus == "Pending")
                {
                    var daysDiff = (DateTime.Now - orderDate).Days;
                    if (daysDiff < 30)
                    {
                        if (daysDiff > 0)
                        {
                            order.Status = "Active";
                            db.SaveChanges();
                        }
                        else
                        {
                            if (orderDate.Hour > 8)
                            {
                                if (orderDate.Hour < 18)
                                {
                                    order.Status = "Active";
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
                    if (currentStatus == "Completed")
                    {
                        return BadRequest("Cannot reactivate completed order");
                    }
                    else
                    {
                        if (currentStatus == "Shipped")
                        {
                            return BadRequest("Cannot change shipped order");
                        }
                        else
                        {
                            order.Status = "Active";
                            db.SaveChanges();
                        }
                    }
                }
            }
            else
            {
                if (status == "Completed")
                {
                    if (currentStatus == "Active")
                    {
                        order.Status = "Completed";
                        db.SaveChanges();
                    }
                    else
                    {
                        if (currentStatus == "Pending")
                        {
                            return BadRequest("Cannot complete pending order");
                        }
                        else
                        {
                            if (currentStatus == "Shipped")
                            {
                                order.Status = "Completed";
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
                    if (status == "Shipped")
                    {
                        if (currentStatus == "Active")
                        {
                            order.Status = "Shipped";
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
                        if (status == "Pending")
                        {
                            if (currentStatus == "Active")
                            {
                                return BadRequest("Cannot revert to pending");
                            }
                            else
                            {
                                order.Status = "Pending";
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

    [HttpGet("report/sales")]
    public async Task<IActionResult> GetSalesReport()
    {
        logger.LogError("Generating sales report");
        var data = new List<object>();
        try
        {
            Thread.Sleep(500);
            logger.LogInformation("Sleeping...");
            var orders = db.Orders
                .Where(o => o.Status != "Pending")
                .ToList();
            logger.LogWarning("Report query executed");
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

            logger.LogError("Total sales: " + total + " count: " + count);
            var report = new
            {
                Orders = data,
                TotalSales = total,
                OrderCount = count,
                Average = count > 0 ? total / count : 0
            };
            var filePath = "C:\\Reports\\sales_report_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            logger.LogInformation("Writing to: " + filePath);
            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = new StreamWriter(filePath);
            file.WriteLine("Sales Report - " + DateTime.Now.ToString());
            file.WriteLine("Total Sales: " + total);
            file.WriteLine("Order Count: " + count);
            file.WriteLine("Average: " + (count > 0 ? total / count : 0));
            file.Close();
            Thread.Sleep(300);
            logger.LogWarning("Report complete");
            return Ok(report);
        }
        catch
        {
            logger.LogInformation("Report error");
            return StatusCode(500);
        }
    }
}