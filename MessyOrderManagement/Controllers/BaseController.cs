using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MessyOrderManagement.Data;

namespace MessyOrderManagement.Controllers;

public abstract class BaseController : ControllerBase
{
    protected readonly ILogger logger;
    protected readonly OrderDbContext db;

    protected BaseController(ILogger logger, OrderDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

    protected IActionResult GetAllEntities<T>(DbSet<T> dbSet, string entityTypeName) where T : class
    {
        logger.LogDebug("Getting all {EntityType}", entityTypeName);
        try
        {
            var entities = dbSet.ToList();
            logger.LogInformation("Retrieved {Count} {EntityType}", entities.Count, entityTypeName);
            return Ok(entities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving {EntityType}", entityTypeName);
            return StatusCode(500);
        }
    }

    protected IActionResult GetEntityById<T>(DbSet<T> dbSet, int id, string entityTypeName) where T : class
    {
        logger.LogDebug("Getting {EntityType} with ID: {EntityId}", entityTypeName, id);
        try
        {
            var entity = dbSet.Find(id);
            if (entity == null)
            {
                logger.LogWarning("{EntityType} {EntityId} not found", entityTypeName, id);
                return NotFound();
            }

            logger.LogInformation("{EntityType} {EntityId} retrieved successfully", entityTypeName, id);
            return Ok(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving {EntityType} {EntityId}", entityTypeName, id);
            return StatusCode(500);
        }
    }

    protected T? FindEntityById<T>(DbSet<T> dbSet, int id, string entityTypeName) where T : class
    {
        try
        {
            var entity = dbSet.Find(id);
            if (entity == null)
            {
                logger.LogWarning("{EntityType} {EntityId} not found", entityTypeName, id);
            }

            return entity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding {EntityType} {EntityId}", entityTypeName, id);
            throw;
        }
    }

    protected IActionResult ExecuteWithErrorHandling(Func<IActionResult> action, string operation, string entityType)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error {Operation} {EntityType}", operation, entityType);
            return StatusCode(500);
        }
    }

    protected IActionResult ExecuteWithErrorHandling<T>(Func<T> action, Func<T, IActionResult> onSuccess, string operation, string entityType)
    {
        try
        {
            var result = action();
            return onSuccess(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error {Operation} {EntityType}", operation, entityType);
            return StatusCode(500);
        }
    }
}
