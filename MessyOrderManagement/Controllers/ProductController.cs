using Microsoft.AspNetCore.Mvc;
using MessyOrderManagement.Models;
using MessyOrderManagement.Data;

namespace MessyOrderManagement.Controllers;

[ApiController]
[Route("api/product")]
public class ProductController : BaseController
{
    public ProductController(ILogger<ProductController> logger, OrderDbContext db)
        : base(logger, db)
    {
    }

    [HttpGet]
    public IActionResult GetAllProducts()
    {
        return GetAllEntities(db.Products, "products");
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
        return ExecuteWithErrorHandling(
            () =>
            {
                if (product.LastUpdated == DateTime.MinValue)
                {
                    product.LastUpdated = DateTime.UtcNow;
                }

                db.Products.Add(product);
                db.SaveChanges();
                logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
                return CreatedAtAction(nameof(GetAllProducts), product);
            },
            "creating",
            $"product: {product.Name}");
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
        return ExecuteWithErrorHandling(
            () =>
            {
                var existing = FindEntityById(db.Products, id, "product");
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
                logger.LogInformation("Product {ProductId} updated successfully", id);
                return Ok(product);
            },
            "updating",
            $"product {id}");
    }
}
