using ProductService.Data;
using ProductService.Models;

namespace ProductService.Repository
{
    public class ProductRepository
    {
        private readonly ProductDbContext _dbContext;

        public ProductRepository(ProductDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddProductAsync(Product product)
        {
            await _dbContext.Products.AddAsync(product);
            _dbContext.SaveChanges();
        }

        public async Task<Product> GetProductByIdAsync(int productId)
        {
            return await _dbContext.Products.FindAsync(productId);
        }

        public async Task UpdateProductAsync(int productId, int quantity)
        {
            Product product = _dbContext.Products.Find(productId);
            if (product != null)
            {
                product.Quantity = quantity;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
