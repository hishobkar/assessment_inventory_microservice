# Inventory Management System - Microservices Assessment Guide

## Overview
A retail business is struggling to keep track of inventory levels and manage customer orders efficiently. The business operates two distinct functions: managing product stock and handling customer orders. Currently, these functions are handled manually, leading to inefficiencies and errors. This project aims to automate these processes using microservices.

## Project Structure
You will develop two microservices:
- **InventoryService** (Manages product stock)
- **OrderService** (Handles customer orders)

Both microservices will interact via REST APIs. The OrderService will verify product availability with InventoryService and update product stock after an order is placed.

## Technology Stack
- **.NET 6+**
- **Entity Framework Core (Code First Approach)**
- **MS SQL Server**
- **Swagger**
- **REST APIs**
- **Microservices Architecture**

## Setup Instructions

### Step 1: Open Project in Visual Studio
1. Navigate to the **Project folder** on the Lab Desktop.
2. Open the folder in **Visual Studio**.
3. Ensure that all dependencies are installed via NuGet.

### Step 2: Setting Up Databases

#### InventoryService
1. Open terminal in **InventoryService** project folder.
2. Run the following commands:
   ```powershell
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

#### OrderService
1. Open terminal in **OrderService** project folder.
2. Run the following commands:
   ```powershell
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

### Step 3: Implement InventoryService

#### Product Model
Located in `Models/Product.cs`:
```csharp
public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

#### InventoryDbContext
Located in `Data/InventoryDbContext.cs`:
```csharp
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) {}
    public DbSet<Product> Products { get; set; }
}
```

#### ProductRepository
Located in `Repositories/ProductRepository.cs`:
```csharp
public class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _context;

    public ProductRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task AddProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task<Product> GetProductByIdAsync(int productId)
    {
        return await _context.Products.FindAsync(productId);
    }

    public async Task UpdateProductStockAsync(int productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product != null)
        {
            product.Quantity = quantity;
            await _context.SaveChangesAsync();
        }
    }
}
```

#### ProductController
Located in `Controllers/ProductController.cs`:
```csharp
[ApiController]
[Route("api/Product")]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductController(IProductRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] Product product)
    {
        await _repository.AddProductAsync(product);
        return StatusCode(201, new { message = "Product created successfully" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _repository.GetProductByIdAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });
        return Ok(product);
    }

    [HttpPut("{id}/Stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] int quantity)
    {
        await _repository.UpdateProductStockAsync(id, quantity);
        return Ok(new { message = "Product stock updated successfully" });
    }
}
```

#### Dependency Injection in `Program.cs`
```csharp
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddTransient<IProductRepository, ProductRepository>();
```

### Step 4: Implement OrderService
#### Order Model
Located in `Models/Order.cs`:
```csharp
public class Order
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
```

#### OrderController
Located in `Controllers/OrderController.cs`:
```csharp
[HttpPost]
public async Task<IActionResult> PlaceOrder([FromBody] Order order)
{
    using (var httpClient = new HttpClient())
    {
        var productResponse = await httpClient.GetAsync($"http://localhost:5118/api/Product/{order.ProductId}");
        if (!productResponse.IsSuccessStatusCode)
            return BadRequest("Product not available.");
        
        var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
        if (product.Quantity < order.Quantity)
            return BadRequest("Insufficient stock for the product.");
        
        var updateStock = new StringContent(JsonConvert.SerializeObject(product.Quantity - order.Quantity), Encoding.UTF8, "application/json");
        await httpClient.PutAsync($"http://localhost:5118/api/Product/{order.ProductId}/Stock", updateStock);
        
        await _repository.AddOrderAsync(order);
        return Ok(new { message = "Order placed successfully." });
    }
}
```

### Step 5: Run and Test the Application
1. Start **InventoryService** (`http://localhost:5118/swagger/index.html`).
2. Start **OrderService** (`http://localhost:5058/swagger/index.html`).
3. Use **Swagger** or Postman to test the endpoints.

### Step 6: Run Test Cases
Verify the following:
- Database setup (`dotnet ef database update` for both services).
- POST Product API (`/api/Product` - Adds a product).
- GET Product API (`/api/Product/{id}` - Retrieves product details).
- PUT Update Stock API (`/api/Product/{id}/Stock` - Updates stock).
- POST Order API (`/api/Order` - Places an order correctly).

# Configuring Dependency Injection in Program.cs

## Dependency Injection Setup for InventoryService

In the `InventoryService` microservice, configure dependency injection in `Program.cs` as follows:

### Steps:
1. **Register the Database Context** using Entity Framework Core.
2. **Register the Repository** as a service.
3. **Configure Controllers and Swagger** for API documentation.

### `Program.cs` Code for `InventoryService`

```csharp
using InventoryService.Data;
using InventoryService.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Database Context
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository
builder.Services.AddTransient<IProductRepository, ProductRepository>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## Dependency Injection Setup for OrderService

In the `OrderService` microservice, configure dependency injection in `Program.cs` as follows:

### Steps:
1. **Register the Database Context** using Entity Framework Core.
2. **Register the Repository** as a service.
3. **Configure HttpClient** for communication with InventoryService.
4. **Configure Controllers and Swagger** for API documentation.

### `Program.cs` Code for `OrderService`

```csharp
using OrderService.Data;
using OrderService.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Database Context
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository
builder.Services.AddTransient<IOrderRepository, OrderRepository>();

// Register HttpClient for calling InventoryService
builder.Services.AddHttpClient();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Summary
- Both microservices register their respective database contexts using `AddDbContext`.
- The repository pattern is implemented using `AddTransient`.
- Swagger is enabled for API documentation.
- `OrderService` uses `AddHttpClient` for making API calls to `InventoryService`.

These configurations ensure that the services are properly injected and available for dependency resolution within the microservices.


---
Ensure all breakpoints are removed before submission.

Happy coding! 🚀

