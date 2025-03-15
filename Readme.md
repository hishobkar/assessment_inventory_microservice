# ProductService Implementation Guide

This guide provides step-by-step instructions to develop the ProductService, including database integration and API implementation.

## Table of Contents
- [1. Create ProductService](#1-create-productservice)
- [2. Define Product Model](#2-define-product-model)
- [3. Implement Product Repository](#3-implement-product-repository)
- [4. Implement Product API](#4-implement-product-api)
- [5. Database Integration](#5-database-integration)
- [6. Testing ProductService](#6-testing-productservice)

## 1. Create ProductService
Create a new .NET Web API project:
```sh
dotnet new webapi -n ProductService
cd ProductService
```

Install required NuGet packages:
```sh
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

## 2. Define Product Model
Create a `Models` folder and add `Product.cs`:
```csharp
public class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Stock { get; set; }
}
```

## 3. Implement Product Repository
Create `Repositories/IProductRepository.cs`:
```csharp
public interface IProductRepository {
    Task<Product> GetProductByIdAsync(int id);
    Task AddProductAsync(Product product);
    Task UpdateProductAsync(Product product);
}
```

Create `Repositories/ProductRepository.cs`:
```csharp
public class ProductRepository : IProductRepository {
    private readonly ProductDbContext _context;
    public ProductRepository(ProductDbContext context) {
        _context = context;
    }
    public async Task<Product> GetProductByIdAsync(int id) => await _context.Products.FindAsync(id);
    public async Task AddProductAsync(Product product) {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateProductAsync(Product product) {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }
}
```

## 4. Implement Product API
Create `Controllers/ProductController.cs`:
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase {
    private readonly IProductRepository _productRepository;
    
    public ProductController(IProductRepository productRepository) {
        _productRepository = productRepository;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id) {
        var product = await _productRepository.GetProductByIdAsync(id);
        if (product == null) return NotFound("Product not available.");
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] Product product) {
        await _productRepository.AddProductAsync(product);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}/Stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] int stock) {
        var product = await _productRepository.GetProductByIdAsync(id);
        if (product == null) return NotFound("Product not available.");
        product.Stock = stock;
        await _productRepository.UpdateProductAsync(product);
        return Ok(product);
    }
}
```

## 5. Database Integration
Create `Data/ProductDbContext.cs`:
```csharp
public class ProductDbContext : DbContext {
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) {}
    public DbSet<Product> Products { get; set; }
}
```

Configure dependency injection in `Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();
app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.Run();
```

## 6. Testing ProductService
1. **Start ProductService** (`http://localhost:5118/swagger/index.html`).
2. **Test Cases**:
   - **Retrieve Product by ID**
   - **Add New Product**
   - **Update Product Stock**

Follow these steps to successfully implement and test ProductService!



# OrderService Implementation Guide

This guide provides step-by-step instructions to develop the OrderService, including database integration and microservice communication with InventoryService.

## Table of Contents
- [1. Create OrderService](#1-create-orderservice)
- [2. Define Order Model](#2-define-order-model)
- [3. Implement Order Repository](#3-implement-order-repository)
- [4. Implement Order API](#4-implement-order-api)
- [5. Implement Microservice Communication](#5-implement-microservice-communication)
- [6. Database Integration](#6-database-integration)
- [7. Testing OrderService](#7-testing-orderservice)

## 1. Create OrderService
Create a new .NET Web API project:
```sh
dotnet new webapi -n OrderService
cd OrderService
```

Install required NuGet packages:
```sh
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

## 2. Define Order Model
Create a `Models` folder and add `Order.cs`:
```csharp
public class Order {
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}
```

## 3. Implement Order Repository
Create `Repositories/IOrderRepository.cs`:
```csharp
public interface IOrderRepository {
    Task AddOrderAsync(Order order);
}
```

Create `Repositories/OrderRepository.cs`:
```csharp
public class OrderRepository : IOrderRepository {
    private readonly OrderDbContext _context;
    public OrderRepository(OrderDbContext context) {
        _context = context;
    }
    public async Task AddOrderAsync(Order order) {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }
}
```

## 4. Implement Order API
Create `Controllers/OrderController.cs`:
```csharp
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase {
    private readonly IOrderRepository _orderRepository;
    private readonly HttpClient _httpClient;

    public OrderController(IOrderRepository orderRepository, HttpClient httpClient) {
        _orderRepository = orderRepository;
        _httpClient = httpClient;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] Order order) {
        var response = await _httpClient.GetAsync($"http://localhost:5118/api/Product/{order.ProductId}");
        if (!response.IsSuccessStatusCode)
            return BadRequest("Product not available.");

        var product = await response.Content.ReadFromJsonAsync<ProductDTO>();
        if (product.Stock < order.Quantity)
            return BadRequest("Insufficient stock for the product.");

        var stockUpdateResponse = await _httpClient.PutAsJsonAsync($"http://localhost:5118/api/Product/{order.ProductId}/Stock", product.Stock - order.Quantity);
        if (!stockUpdateResponse.IsSuccessStatusCode)
            return StatusCode(500, "Error updating stock.");

        await _orderRepository.AddOrderAsync(order);
        return Ok(new { message = "Order placed successfully." });
    }
}

public class ProductDTO {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Stock { get; set; }
}
```

## 5. Implement Microservice Communication
- **Retrieves product details** using an HTTP GET request.
- **Checks stock availability** before placing an order.
- **Updates stock** using an HTTP PUT request.
- **Saves order** if stock update is successful.

## 6. Database Integration
Create `Data/OrderDbContext.cs`:
```csharp
public class OrderDbContext : DbContext {
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) {}
    public DbSet<Order> Orders { get; set; }
}
```

Configure dependency injection in `Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddHttpClient();

var app = builder.Build();
app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.Run();
```

## 7. Testing OrderService
1. **Start InventoryService** (`http://localhost:5118/swagger/index.html`).
2. **Start OrderService** (`http://localhost:5058/swagger/index.html`).
3. **Test Cases**:
   - **Successful Order Placement**
   - **Product Not Found**
   - **Insufficient Stock**

Follow these steps to successfully implement and test OrderService!

