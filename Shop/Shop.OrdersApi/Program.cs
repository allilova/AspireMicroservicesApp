using Microsoft.EntityFrameworkCore;
using Shop.OrdersApi.Clients;
using Shop.OrdersApi.Data;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<OrdersDbContext>(o => o.UseSqlite("Data Source=orders.db"));
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new("https+http://catalog-api"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.EnsureCreated();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var orders = app.MapGroup("/orders");

orders.MapGet("/", async (OrdersDbContext db) =>
    await db.Orders.Include(o => o.Items).OrderByDescending(o => o.Id).ToListAsync());

orders.MapGet("/{id:int}", async (int id, OrdersDbContext db) =>
    await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id) is Order o
        ? Results.Ok(o)
        : Results.NotFound());

orders.MapPost("/", async (CreateOrderRequest request, OrdersDbContext db, CatalogClient catalog) =>
{
    if (string.IsNullOrWhiteSpace(request.CustomerName))
        return Results.BadRequest("Името на клиента е задължително.");
    if (request.Items is null || request.Items.Count == 0)
        return Results.BadRequest("Поръчката трябва да има поне един артикул.");

    var order = new Order { CustomerName = request.CustomerName };

    foreach (var line in request.Items)
    {
        if (line.Quantity <= 0)
            return Results.BadRequest("Количеството трябва да е поне 1.");

        ProductDto? product;
        try
        {
            product = await catalog.GetProductAsync(line.ProductId);
        }
        catch (Exception)
        {
            // каталогът не отговаря: спрян е, бавен е или е недостъпен
            return Results.Problem("Каталогът не е достъпен. Опитайте по-късно.", statusCode: 503);
        }

        if (product is null)
            return Results.BadRequest($"Продукт с номер {line.ProductId} не съществува.");

        order.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = line.Quantity
        });
    }

    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
});

orders.MapPut("/{id:int}/status", async (int id, UpdateStatusRequest request, OrdersDbContext db) =>
{
    string[] allowed = ["New", "Confirmed", "Shipped", "Cancelled"];
    if (!allowed.Contains(request.Status))
        return Results.BadRequest($"Позволени статуси: {string.Join(", ", allowed)}");

    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    order.Status = request.Status;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDefaultEndpoints();

app.Run();

record CreateOrderRequest(string CustomerName, List<OrderLineRequest> Items);
record OrderLineRequest(int ProductId, int Quantity);
record UpdateStatusRequest(string Status);