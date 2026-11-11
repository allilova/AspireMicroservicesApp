using MongoDB.Bson;
using MongoDB.Driver;
using Shop.OrdersApi.Clients;
using Shop.OrdersApi.Data;
using Shop.OrdersApi.Notifications;
using System.Diagnostics;
using Shop.OrdersApi.Telemetry;

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
builder.Services.AddHttpClient("notifications", c => c.BaseAddress = new("https+http://notifications-api"));
builder.Services.AddSingleton<NotificationQueue>();
builder.Services.AddHostedService<NotificationSender>();

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

orders.MapPost("/", async (CreateOrderRequest request, IMongoCollection<Order> col, CatalogClient catalog, NotificationQueue notifications, ILogger<Program> logger) =>
{
    // основен span на цялата поръчка
    using var activity = OrdersTelemetry.Source.StartActivity("CreateOrder");
    activity?.SetTag("order.lines", request.Items?.Count ?? 0);

    // записва отказана поръчка в метриките и в трасето
    void MarkRejected(string reason)
    {
        OrdersTelemetry.OrdersRejected.Add(1, new KeyValuePair<string, object?>("reason", reason));
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    if (string.IsNullOrWhiteSpace(request.CustomerName))
    {
        MarkRejected("validation");
        return Results.BadRequest("Името на клиента е задължително.");
    }
    if (request.Items is null || request.Items.Count == 0)
    {
        MarkRejected("validation");
        return Results.BadRequest("Поръчката трябва да има поне един артикул.");
    }
    if (request.Items.Any(l => l.Quantity <= 0))
    {
        MarkRejected("validation");
        return Results.BadRequest("Количеството трябва да е поне 1.");
    }

    var order = new Order { CustomerName = request.CustomerName };
    var reserved = new List<(string ProductId, int Quantity)>();

    // компенсиращо действие: връща всичко, което вече е резервирано
    async Task ReleaseAllAsync()
    {
        if (reserved.Count == 0) return;

        using var compensation = OrdersTelemetry.Source.StartActivity("CompensateStock");
        compensation?.SetTag("order.reserved_lines", reserved.Count);
        OrdersTelemetry.StockCompensations.Add(1);

        foreach (var (productId, quantity) in reserved)
        {
            try
            {
                await catalog.ReleaseAsync(productId, quantity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Неуспешно връщане на {Quantity} бр. от продукт {ProductId}", quantity, productId);
            }
        }
    }

    try
    {
        foreach (var line in request.Items)
        {
            ReserveResult result;
            using (var reserve = OrdersTelemetry.Source.StartActivity("ReserveStock"))
            {
                reserve?.SetTag("product.id", line.ProductId);
                reserve?.SetTag("quantity", line.Quantity);
                result = await catalog.ReserveAsync(line.ProductId, line.Quantity);
                reserve?.SetTag("result", result.Status.ToString());
            }

            if (result.Status == ReserveStatus.NotFound)
            {
                await ReleaseAllAsync();
                MarkRejected("product_not_found");
                return Results.BadRequest($"Продукт с номер {line.ProductId} не съществува.");
            }

            if (result.Status == ReserveStatus.Insufficient)
            {
                await ReleaseAllAsync();
                MarkRejected("insufficient_stock");
                return Results.Conflict($"Недостатъчна наличност за „{result.Product?.Name}“ (налични: {result.Product?.Stock}).");
            }

            reserved.Add((line.ProductId, line.Quantity));
            order.Items.Add(new OrderItem
            {
                ProductId = result.Product!.Id,
                ProductName = result.Product.Name,
                UnitPrice = result.Product.Price,
                Quantity = line.Quantity
            });
        }

        await col.InsertOneAsync(order);

        OrdersTelemetry.OrdersCreated.Add(1);
        OrdersTelemetry.OrderValue.Record((double)order.Total);
        activity?.SetTag("order.id", order.Id);

        notifications.TryEnqueue(new OrderNotification(
            "OrderCreated",
            $"Нова поръчка от {order.CustomerName} на стойност {order.Total:N2} €",
            order.Id));

        return Results.Created($"/orders/{order.Id}", order);
    }
    catch (Exception ex)
    {
        // каталогът е паднал по средата или записът на поръчката е неуспешен
        logger.LogError(ex, "Грешка при създаване на поръчка, връщам резервираните количества");
        activity?.AddException(ex);
        await ReleaseAllAsync();
        MarkRejected("error");
        return Results.Problem("Поръчката не може да бъде обработена. Опитайте по-късно.", statusCode: 503);
    }
});


orders.MapPut("/{id}/status", async (string id, UpdateStatusRequest request, IMongoCollection<Order> col, NotificationQueue notifications) =>
{
    string[] allowed = ["New", "Confirmed", "Shipped", "Cancelled"];
    if (!allowed.Contains(request.Status))
        return Results.BadRequest($"Позволени статуси: {string.Join(", ", allowed)}");

    if (!ObjectId.TryParse(id, out _)) return Results.NotFound();

    var result = await col.UpdateOneAsync(
        o => o.Id == id,
        Builders<Order>.Update.Set(o => o.Status, request.Status));

    if (result.MatchedCount == 0) return Results.NotFound();

    notifications.TryEnqueue(new OrderNotification(
        "OrderStatusChanged",
        $"Поръчка #{id[^6..].ToUpperInvariant()} е със статус {request.Status}",
        id));

    return Results.NoContent();
});

app.MapDefaultEndpoints();

app.Run();

record CreateOrderRequest(string CustomerName, List<OrderLineRequest> Items);
record OrderLineRequest(string ProductId, int Quantity);
record UpdateStatusRequest(string Status);