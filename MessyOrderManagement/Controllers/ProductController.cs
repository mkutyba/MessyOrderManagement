using Microsoft.AspNetCore.Mvc;
using MessyOrderManagement.Models;
using MessyOrderManagement.Data;

namespace MessyOrderManagement.Controllers;

[ApiController]
[Route("api/product")]
public class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> logger;
    private readonly OrderDbContext db;

    public ProductController(ILogger<ProductController> logger, OrderDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

    [HttpGet]
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

    [HttpPost]
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

    [HttpPut("{id}")]
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
}
