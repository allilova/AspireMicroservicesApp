using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using Shop.CatalogApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// регистрира IMongoClient (с health check и телеметрия) от connection string "mongodb"
builder.AddMongoDBClient("mongodb");
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var dbName = sp.GetRequiredService<IConfiguration>()["Mongo:Database"] ?? "catalogdb";
    return client.GetDatabase(dbName).GetCollection<Product>("products");
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ProductsCache>();

var app = builder.Build();

// примерни продукти, ако колекцията е празна
var collection = app.Services.GetRequiredService<IMongoCollection<Product>>();
if (await collection.CountDocumentsAsync(FilterDefinition<Product>.Empty) == 0)
{
    await collection.InsertManyAsync(new[]
    {
        new Product { Name = "Лаптоп 15\"", Category = "Компютри", Price = 1299.00m, Description = "Лаптоп за работа и учене" },
        new Product { Name = "Безжична мишка", Category = "Аксесоари", Price = 29.90m },
        new Product { Name = "Механична клавиатура", Category = "Аксесоари", Price = 89.00m },
        new Product { Name = "Монитор 27\"", Category = "Компютри", Price = 349.00m }
    });
}

var products = app.MapGroup("/products");

products.MapGet("/", async (IMongoCollection<Product> col, IMemoryCache cache, ProductsCache pc, string? search, string? category) =>
{
    var key = $"products:{search}:{category}";
    return await cache.GetOrCreateAsync(key, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        entry.ExpirationTokens.Add(pc.GetToken());

        var filter = Builders<Product>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
            filter &= Builders<Product>.Filter.Regex(p => p.Name, new BsonRegularExpression(Regex.Escape(search), "i"));
        if (!string.IsNullOrWhiteSpace(category))
            filter &= Builders<Product>.Filter.Eq(p => p.Category, category);

        return await col.Find(filter).SortBy(p => p.Name).ToListAsync();
    });
});

products.MapGet("/{id}", async (string id, IMongoCollection<Product> col) =>
{
    if (!ObjectId.TryParse(id, out _)) return Results.NotFound();
    var p = await col.Find(x => x.Id == id).FirstOrDefaultAsync();
    return p is null ? Results.NotFound() : Results.Ok(p);
});

products.MapPost("/", async (Product input, IMongoCollection<Product> col, ProductsCache pc) =>
{
    input.Id = null;
    await col.InsertOneAsync(input);
    pc.Invalidate();
    return Results.Created($"/products/{input.Id}", input);
});

products.MapPut("/{id}", async (string id, Product input, IMongoCollection<Product> col, ProductsCache pc) =>
{
    if (!ObjectId.TryParse(id, out _)) return Results.NotFound();
    input.Id = id;
    var result = await col.ReplaceOneAsync(p => p.Id == id, input);
    if (result.MatchedCount == 0) return Results.NotFound();
    pc.Invalidate();
    return Results.NoContent();
});

products.MapDelete("/{id}", async (string id, IMongoCollection<Product> col, ProductsCache pc) =>
{
    if (!ObjectId.TryParse(id, out _)) return Results.NotFound();
    var result = await col.DeleteOneAsync(p => p.Id == id);
    if (result.DeletedCount == 0) return Results.NotFound();
    pc.Invalidate();
    return Results.NoContent();
});

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}