using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Shop.Tests;

public class ShopIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);

    private record CatalogProduct(string Id, string Name, string Category, string? Description, decimal Price, int Stock);
    private record OrderLineResult(string ProductId, string ProductName, decimal UnitPrice, int Quantity);
    private record OrderResult(string Id, string CustomerName, string Status, List<OrderLineResult> Items, decimal Total);

    private static async Task<DistributedApplication> StartAppAsync(CancellationToken ct)
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Shop_AppHost>(["--DbSuffix=_test"], ct);
        var app = await appHost.BuildAsync(ct).WaitAsync(Timeout, ct);
        await app.StartAsync(ct).WaitAsync(Timeout, ct);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("catalog-api", ct).WaitAsync(Timeout, ct);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("orders-api", ct).WaitAsync(Timeout, ct);
        return app;
    }

    private static async Task<CatalogProduct> CreateProductAsync(HttpClient catalog, int stock, decimal price, CancellationToken ct)
    {
        var response = await catalog.PostAsJsonAsync("/products",
            new { name = "Тестов продукт", category = "Тест", price, stock }, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CatalogProduct>(ct))!;
    }

    private static Task<CatalogProduct?> GetProductAsync(HttpClient catalog, string id, CancellationToken ct)
        => catalog.GetFromJsonAsync<CatalogProduct>($"/products/{id}", ct);

    [Fact]
    public async Task Catalog_returns_seeded_products()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var app = await StartAppAsync(cts.Token);
        using var catalog = app.CreateHttpClient("catalog-api");

        var products = await catalog.GetFromJsonAsync<List<CatalogProduct>>("/products", cts.Token);

        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }

    [Fact]
    public async Task Creating_order_takes_price_from_catalog()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var app = await StartAppAsync(cts.Token);
        using var catalog = app.CreateHttpClient("catalog-api");
        using var orders = app.CreateHttpClient("orders-api");
        var product = await CreateProductAsync(catalog, stock: 100, price: 10m, cts.Token);

        var response = await orders.PostAsJsonAsync("/orders", new
        {
            customerName = "Тест",
            items = new[] { new { productId = product.Id, quantity = 2 } }
        }, cts.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResult>(cts.Token);
        Assert.NotNull(order);
        Assert.Equal(10m, order.Items[0].UnitPrice);
        Assert.Equal(20m, order.Total);
    }

    [Fact]
    public async Task Creating_order_reduces_stock()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var app = await StartAppAsync(cts.Token);
        using var catalog = app.CreateHttpClient("catalog-api");
        using var orders = app.CreateHttpClient("orders-api");
        var product = await CreateProductAsync(catalog, stock: 10, price: 5m, cts.Token);

        var response = await orders.PostAsJsonAsync("/orders", new
        {
            customerName = "Тест",
            items = new[] { new { productId = product.Id, quantity = 3 } }
        }, cts.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var after = await GetProductAsync(catalog, product.Id, cts.Token);
        Assert.Equal(7, after!.Stock);
    }

    [Fact]
    public async Task Order_exceeding_stock_is_rejected_and_stock_unchanged()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var app = await StartAppAsync(cts.Token);
        using var catalog = app.CreateHttpClient("catalog-api");
        using var orders = app.CreateHttpClient("orders-api");
        var product = await CreateProductAsync(catalog, stock: 2, price: 5m, cts.Token);

        var response = await orders.PostAsJsonAsync("/orders", new
        {
            customerName = "Тест",
            items = new[] { new { productId = product.Id, quantity = 5 } }
        }, cts.Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var after = await GetProductAsync(catalog, product.Id, cts.Token);
        Assert.Equal(2, after!.Stock);
    }

    [Fact]
    public async Task Failed_order_releases_already_reserved_stock()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var app = await StartAppAsync(cts.Token);
        using var catalog = app.CreateHttpClient("catalog-api");
        using var orders = app.CreateHttpClient("orders-api");
        var plenty = await CreateProductAsync(catalog, stock: 10, price: 5m, cts.Token);
        var scarce = await CreateProductAsync(catalog, stock: 1, price: 5m, cts.Token);

        // първият ред успява (резервира 4), вторият се проваля, и 4-те трябва да се върнат
        var response = await orders.PostAsJsonAsync("/orders", new
        {
            customerName = "Тест",
            items = new[]
            {
                new { productId = plenty.Id, quantity = 4 },
                new { productId = scarce.Id, quantity = 5 }
            }
        }, cts.Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var after = await GetProductAsync(catalog, plenty.Id, cts.Token);
        Assert.Equal(10, after!.Stock);
    }

    [Fact]
    public async Task Order_with_unknown_product_is_rejected()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var app = await StartAppAsync(cts.Token);
        using var orders = app.CreateHttpClient("orders-api");

        var response = await orders.PostAsJsonAsync("/orders", new
        {
            customerName = "Тест",
            items = new[] { new { productId = "000000000000000000000000", quantity = 1 } }
        }, cts.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Order_with_zero_quantity_is_rejected()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var app = await StartAppAsync(cts.Token);
        using var orders = app.CreateHttpClient("orders-api");

        var response = await orders.PostAsJsonAsync("/orders", new
        {
            customerName = "Тест",
            items = new[] { new { productId = "000000000000000000000000", quantity = 0 } }
        }, cts.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}