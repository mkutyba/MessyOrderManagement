using Microsoft.AspNetCore.Mvc;
using MessyOrderManagement.Data;
using MessyOrderManagement.Constants;
using MessyOrderManagement.Repositories;
using MessyOrderManagement.Models;

namespace MessyOrderManagement.Controllers;

[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly ILogger<ReportController> logger;
    private readonly IOrderRepository orderRepository;

    public ReportController(ILogger<ReportController> logger, IOrderRepository orderRepository)
    {
        this.logger = logger;
        this.orderRepository = orderRepository;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport()
    {
        logger.LogInformation("Generating sales report");
        var data = new List<object>();
        try
        {
            await Task.Delay(500);
            // Single query with eager loading - no N+1 problem!
            var orders = await orderRepository.GetSalesReportDataAsync();
            
            logger.LogDebug("Retrieved {Count} non-pending orders for report", orders.Count);
            
            var total = 0.0m;
            var count = 0;
            foreach (var order in orders)
            {
                // No more queries in loop - data already loaded!
                var x = new
                {
                    OrderId = order.Id,
                    Date = order.Date,
                    Total = order.Total,
                    Customer = order.Customer?.Name ?? "",
                    Product = order.Product?.Name ?? ""
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
            var reportsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Reports");
            var filePath = Path.Combine(reportsDirectory, $"sales_report_{DateTime.UtcNow:yyyyMMdd}.txt");
            logger.LogDebug("Writing report to file: {FilePath}", filePath);
            if (!Directory.Exists(reportsDirectory))
            {
                Directory.CreateDirectory(reportsDirectory);
            }

            using (var file = new StreamWriter(filePath))
            {
                await file.WriteLineAsync($"Sales Report - {DateTime.UtcNow}");
                await file.WriteLineAsync($"Total Sales: {total}");
                await file.WriteLineAsync($"Order Count: {count}");
                await file.WriteLineAsync($"Average: {(count > 0 ? total / count : 0)}");
            }
            
            await Task.Delay(300);
            logger.LogInformation("Sales report generated successfully");
            return Ok(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating sales report");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while generating the sales report",
                Details = ex.Message
            });
        }
    }
}
