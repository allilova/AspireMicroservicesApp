using MongoDB.Bson;
using MongoDB.Driver;
using Shop.OrdersApi.Clients;
using Shop.OrdersApi.Data;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.AddMongoDBClient("mongodb");
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var dbName = sp.GetRequiredService<IConfiguration>()["Mongo:Database"] ?? "ordersdb";
    return client.GetDatabase(dbName).GetCollection<Order>("orders");
});
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new("https+http://catalog-api"));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var orders = app.MapGroup("/orders");

orders.MapGet("/", async (IMongoCollection<Order> col) =>
    await col.Find(FilterDefinition<Order>.Empty).SortByDescending(o => o.CreatedAt).ToListAsync());

orders.MapGet("/{id}", async (string id, IMongoCollection<Order> col) =>
{
    if (!ObjectId.TryParse(id, out _)) return Results.NotFound();
    var o = await col.Find(x => x.Id == id).FirstOrDefaultAsync();
    return o is null ? Results.NotFound() : Results.Ok(o);
});

orders.MapPost("/", async (CreateOrderRequest request, IMongoCollection<Order> col, CatalogClient catalog) =>
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

    await col.InsertOneAsync(order);
    return Results.Created($"/orders/{order.Id}", order);
});

orders.MapPut("/{id}/status", async (string id, UpdateStatusRequest request, IMongoCollection<Order> col) =>
{
    string[] allowed = ["New", "Confirmed", "Shipped", "Cancelled"];
    if (!allowed.Contains(request.Status))
        return Results.BadRequest($"Позволени статуси: {string.Join(", ", allowed)}");

    if (!ObjectId.TryParse(id, out _)) return Results.NotFound();

    var result = await col.UpdateOneAsync(
        o => o.Id == id,
        Builders<Order>.Update.Set(o => o.Status, request.Status));

    return result.MatchedCount == 0 ? Results.NotFound() : Results.NoContent();
});

app.MapDefaultEndpoints();

app.Run();

record CreateOrderRequest(string CustomerName, List<OrderLineRequest> Items);
record OrderLineRequest(string ProductId, int Quantity);
record UpdateStatusRequest(string Status);