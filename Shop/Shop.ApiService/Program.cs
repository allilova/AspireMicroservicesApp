using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shop.CatalogApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<CatalogDbContext>(o => o.UseSqlite("Data Source=catalog.db"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ProductsCache>();
var app = builder.Build();
// създава базата и добавя примерни продукти
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.EnsureCreated();
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Лаптоп 15\"", Category = "Компютри", Price = 1299.00m, Description = "Лаптоп за работа и учене" },
            new Product { Name = "Безжична мишка", Category = "Аксесоари", Price = 29.90m },
            new Product { Name = "Механична клавиатура", Category = "Аксесоари", Price = 89.00m },
            new Product { Name = "Монитор 27\"", Category = "Компютри", Price = 349.00m });
        db.SaveChanges();
    }
}

var products = app.MapGroup("/products");

products.MapGet("/", async (CatalogDbContext db, IMemoryCache cache, ProductsCache pc, string? search, string? category) =>
{
    var key = $"products:{search}:{category}";
    return await cache.GetOrCreateAsync(key, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        entry.ExpirationTokens.Add(pc.GetToken());

        var query = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);
        return await query.OrderBy(p => p.Name).ToListAsync();
    });
});

products.MapGet("/{id:int}", async (int id, CatalogDbContext db) =>
    await db.Products.FindAsync(id) is Product p ? Results.Ok(p) : Results.NotFound());

products.MapPost("/", async (Product input, CatalogDbContext db, ProductsCache pc) =>
{
    input.Id = 0;
    db.Products.Add(input);
    await db.SaveChangesAsync();
    pc.Invalidate();
    return Results.Created($"/products/{input.Id}", input);
});

products.MapPut("/{id:int}", async (int id, Product input, CatalogDbContext db, ProductsCache pc) =>
{
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();
    p.Name = input.Name;
    p.Category = input.Category;
    p.Description = input.Description;
    p.Price = input.Price;
    await db.SaveChangesAsync();
    pc.Invalidate();                 
    return Results.NoContent();
});

products.MapDelete("/{id:int}", async (int id, CatalogDbContext db, ProductsCache pc) =>
{
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();
    db.Products.Remove(p);
    await db.SaveChangesAsync();
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
