using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Repository;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly OrderRepository _orderRepository;
        private readonly HttpClient _httpClient;

        public OrderController(OrderRepository orderRepository, HttpClient httpClient)
        {
            _orderRepository = orderRepository;
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] Order order)
        {
            if (order == null || order.Quantity <= 0)
            {
                return BadRequest(new { message = "Invalid order request" });
            }
            var response = await _httpClient.GetAsync("https://localhost:5118/api/Product/" + order.ProductId);
            if(!response.IsSuccessStatusCode)
            {
                return NotFound("Product not available.");
            }

            var product = await response.Content.ReadFromJsonAsync<ProductDTO>();
            if (product.Quantity < order.Quantity)
            {
                return BadRequest("Insufficient stock for the product.");
            }

            var stockUpdateResponse = await _httpClient.PutAsJsonAsync($"https://localhost:5118/api/Product/{order.ProductId}/Stock", product.Quantity - order.Quantity);
            if (!stockUpdateResponse.IsSuccessStatusCode)
                return StatusCode(500, "Failed to update stock");

            await _orderRepository.AddOrderAsync(order);
            return Ok(new { message = "Order placed successfully" });
        }
    }

    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}
