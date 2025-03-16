using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using ProductService.Repository;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly ProductRepository _productRepository;

        public ProductController(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound( new { messsage = "Product not found" });
            }
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] Product product)
        {
            if(product == null)
            {
                return BadRequest( new { message = "Invalid product data" });
            }
            await _productRepository.AddProductAsync(product);
            return StatusCode(201, new { message = "Product created successfully" });
        }

        [HttpPut("{id}/Stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] int stock)
        {
            if(stock < 0)
            {
                return BadRequest(new { message = "Stock quantity cannot be negative" });
            }
            await _productRepository.UpdateProductAsync(id, stock);
            return StatusCode(200, new { message = "Product stock updated successfully" });
        }
    }
}
