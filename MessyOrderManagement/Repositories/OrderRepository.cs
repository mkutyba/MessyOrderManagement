using Microsoft.EntityFrameworkCore;
using MessyOrderManagement.Data;
using MessyOrderManagement.Models;
using MessyOrderManagement.Constants;

namespace MessyOrderManagement.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders.ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task DeleteAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Order>> GetSalesReportDataAsync()
    {
        // Eagerly load related data to avoid N+1 problem
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Product)
            .Where(o => o.Status != OrderConstants.StatusPending)
            .ToListAsync();
    }
}
