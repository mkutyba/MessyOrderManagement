using Microsoft.AspNetCore.Mvc;
using MessyOrderManagement.Data;
using MessyOrderManagement.Constants;

namespace MessyOrderManagement.Controllers;

[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly ILogger<ReportController> logger;
    private readonly OrderDbContext db;

    public ReportController(ILogger<ReportController> logger, OrderDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport()
    {
        logger.LogInformation("Generating sales report");
        var data = new List<object>();
        try
        {
            await Task.Delay(500);
            var orders = db.Orders
                .Where(o => o.Status != OrderConstants.StatusPending)
                .ToList();
            
            logger.LogDebug("Retrieved {Count} non-pending orders for report", orders.Count);
            
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

            logger.LogInformation("Sales report calculated: Total sales {TotalSales}, Order count {OrderCount}", 
                total, count);
            
            var report = new
            {
                Orders = data,
                TotalSales = total,
                OrderCount = count,
                Average = count > 0 ? total / count : 0
            };
            var filePath = "C:\\Reports\\sales_report_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            logger.LogDebug("Writing report to file: {FilePath}", filePath);
            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var file = new StreamWriter(filePath))
            {
                file.WriteLine("Sales Report - " + DateTime.Now.ToString());
                file.WriteLine("Total Sales: " + total);
                file.WriteLine("Order Count: " + count);
                file.WriteLine("Average: " + (count > 0 ? total / count : 0));
            }
            
            await Task.Delay(300);
            logger.LogInformation("Sales report generated successfully");
            return Ok(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating sales report");
            return StatusCode(500);
        }
    }
}
