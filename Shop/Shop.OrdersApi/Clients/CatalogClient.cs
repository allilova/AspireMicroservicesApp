using System.Net;

namespace Shop.OrdersApi.Clients;

public enum ReserveStatus { Ok, NotFound, Insufficient }

public record ReserveResult(ReserveStatus Status, ProductDto? Product);

public class CatalogClient(HttpClient http)
{
    public async Task<ReserveResult> ReserveAsync(string id, int quantity, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"/products/{Uri.EscapeDataString(id)}/reserve", new { quantity }, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new(ReserveStatus.NotFound, null);
        if (response.StatusCode == HttpStatusCode.Conflict)
            return new(ReserveStatus.Insufficient, await response.Content.ReadFromJsonAsync<ProductDto>(ct));

        response.EnsureSuccessStatusCode();
        return new(ReserveStatus.Ok, await response.Content.ReadFromJsonAsync<ProductDto>(ct));
    }

    public async Task ReleaseAsync(string id, int quantity, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"/products/{Uri.EscapeDataString(id)}/release", new { quantity }, ct);
        response.EnsureSuccessStatusCode();
    }
}

public record ProductDto(string Id, string Name, string Category, string? Description, decimal Price, int Stock);