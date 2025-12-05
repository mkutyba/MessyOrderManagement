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

    [HttpPost]
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
            return Created($"/api/customer/{customer.Id}", customer);
        }
        catch (Exception ex)
        {
            logger.LogInformation("Error creating customer: " + ex.Message);
            return StatusCode(500);
        }
    }

    [HttpPut("{id}")]
    public IActionResult UpdateCustomer(int id, [FromBody] Customer customer)
    {
        if (customer == null)
        {
            return BadRequest();
        }

        if (id <= 0)
        {
            logger.LogWarning($"Invalid customer ID {id} provided for update");
            return BadRequest();
        }

        try
        {
            var existing = db.Customers.FirstOrDefault(c => c.Id == id);
            if (existing == null)
            {
                var allCustomers = db.Customers.ToList();
                logger.LogWarning($"Customer with ID {id} not found for update. Total customers in DB: {allCustomers.Count}. Existing IDs: {string.Join(", ", allCustomers.Select(c => c.Id))}");
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
        }
        catch (Exception ex)
        {
            logger.LogError($"Error updating customer {id}: {ex.Message}");
            logger.LogError($"Stack trace: {ex.StackTrace}");
            return StatusCode(500);
        }

        return Ok(customer);
    }
}
