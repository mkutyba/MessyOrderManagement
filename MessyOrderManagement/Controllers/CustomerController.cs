using Microsoft.AspNetCore.Mvc;
using MessyOrderManagement.Models;
using MessyOrderManagement.Data;

namespace MessyOrderManagement.Controllers;

[ApiController]
[Route("api/customer")]
public class CustomerController : BaseController
{
    public CustomerController(ILogger<CustomerController> logger, OrderDbContext db, IHostEnvironment environment)
        : base(logger, db, environment)
    {
    }

    [HttpGet]
    public IActionResult GetAllCustomers()
    {
        return GetAllEntities(db.Customers, "customers");
    }

    [HttpPost]
    public IActionResult CreateCustomer([FromBody] Customer? customer)
    {
        if (customer == null)
        {
            logger.LogWarning("CreateCustomer called with null customer");
            return BadRequest();
        }

        return ExecuteWithErrorHandling(
            () =>
            {
                if (customer.CreatedDate == DateTime.MinValue)
                {
                    customer.CreatedDate = DateTime.UtcNow;
                }

                db.Customers.Add(customer);
                db.SaveChanges();
                logger.LogInformation("Customer created successfully with ID: {CustomerId}", customer.Id);
                return Created($"/api/customer/{customer.Id}", customer);
            },
            "creating",
            "customer");
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

        return ExecuteWithErrorHandling(
            () =>
            {
                var existing = FindEntityById(db.Customers, id, "customer");
                if (existing == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        Message = $"customer with ID {id} not found"
                    });
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
                return Ok(customer);
            },
            "updating",
            $"customer {id}");
    }
}
