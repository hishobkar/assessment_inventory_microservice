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
            if (product == null) return NotFound("Product not available.");
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] Product product)
        {
            await _productRepository.AddProductAsync(product);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        [HttpPut("{id}/Stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] int stock)
        {
            await _productRepository.UpdateProductAsync(id, stock);
            return Ok("Product updated successfully");
        }
    }
}
