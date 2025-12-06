using Microsoft.AspNetCore.Mvc;
using MessyOrderManagement.Models;
using MessyOrderManagement.Data;

namespace MessyOrderManagement.Controllers;

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
    public IActionResult GetAllCustomers()
    {
        logger.LogDebug("Getting all customers");
        var customers = new List<Customer>();
        try
        {
            customers = db.Customers.ToList();
            logger.LogInformation("Retrieved {Count} customers", customers.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving customers");
            return StatusCode(500);
        }

        return Ok(customers);
    }

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
            // Preserve the original CreatedDate - do not overwrite it
            db.SaveChanges();

            // Return the updated entity with correct ID and CreatedDate
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
}
