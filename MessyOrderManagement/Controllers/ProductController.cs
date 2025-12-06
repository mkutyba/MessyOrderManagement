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
        logger.LogDebug("Getting all products");
        var products = new List<Product>();
        try
        {
            products = db.Products.ToList();
            logger.LogInformation("Retrieved {Count} products", products.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products");
            return StatusCode(500);
        }

        return Ok(products);
    }

    [HttpPost]
    public IActionResult CreateProduct([FromBody] Product? product)
    {
        if (product == null)
        {
            logger.LogWarning("CreateProduct called with null product");
            return BadRequest();
        }

        logger.LogDebug("Creating product: {ProductName}", product.Name);
        try
        {
            if (product.LastUpdated == DateTime.MinValue)
            {
                product.LastUpdated = DateTime.UtcNow;
            }

            db.Products.Add(product);
            db.SaveChanges();
            logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product: {ProductName}", product.Name);
            return StatusCode(500);
        }

        return CreatedAtAction(nameof(GetAllProducts), product);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateProduct(int id, [FromBody] Product? product)
    {
        if (product == null)
        {
            logger.LogWarning("UpdateProduct called with null product");
            return BadRequest();
        }

        logger.LogDebug("Updating product {ProductId}", id);
        try
        {
            var existing = db.Products.FirstOrDefault(p => p.Id == id);
            if (existing == null)
            {
                logger.LogWarning("Product {ProductId} not found for update", id);
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
            logger.LogInformation("Product {ProductId} updated successfully", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500);
        }

        return Ok(product);
    }
}
