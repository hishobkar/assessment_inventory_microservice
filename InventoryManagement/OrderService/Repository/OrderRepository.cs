using OrderService.Data;
using OrderService.Models;

namespace OrderService.Repository
{
    public class OrderRepository
    {
        private readonly OrderDbContext _dbContext;

        public OrderRepository(OrderDbContext orderDbContext)
        {
            _dbContext = orderDbContext;
        }

        public async Task AddOrderAsync(Order order)
        {
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();
        }
    }
}
