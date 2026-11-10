using System.Net;

namespace Shop.OrdersApi.Clients;

public class CatalogClient(HttpClient http)
{
    public async Task<ProductDto?> GetProductAsync(int id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/products/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(ct);
    }
}

public record ProductDto(int Id, string Name, string Category, string? Description, decimal Price);