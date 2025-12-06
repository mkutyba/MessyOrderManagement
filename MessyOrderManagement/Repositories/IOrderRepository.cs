using MessyOrderManagement.Models;

namespace MessyOrderManagement.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<Order> AddAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task DeleteAsync(int id);
    Task<List<Order>> GetSalesReportDataAsync();
}
