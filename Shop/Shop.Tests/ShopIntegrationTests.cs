using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Shop.Tests;

public class ShopIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);

    private record CatalogProduct(string Id, string Name, string Category, string? Description, decimal Price);
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

        var products = await catalog.GetFromJsonAsync<List<CatalogProduct>>("/products", cts.Token);
        var product = products!.First();

        var response = await orders.PostAsJsonAsync("/orders", new
        {
            customerName = "Тест",
            items = new[] { new { productId = product.Id, quantity = 2 } }
        }, cts.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResult>(cts.Token);
        Assert.NotNull(order);
        Assert.Equal(product.Price, order.Items[0].UnitPrice);
        Assert.Equal(product.Price * 2, order.Total);
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