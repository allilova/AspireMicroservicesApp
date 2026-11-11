namespace Shop.Web;

public class CatalogApiClient(HttpClient http)
{
    public async Task<List<ProductDto>> GetProductsAsync(string? search = null, string? category = null, CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) parts.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category)) parts.Add($"category={Uri.EscapeDataString(category)}");

        var url = parts.Count > 0 ? "/products?" + string.Join("&", parts) : "/products";
        return await http.GetFromJsonAsync<List<ProductDto>>(url, ct) ?? [];
    }
    public async Task<ProductDto?> CreateProductAsync(ProductInput input, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/products", input, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(ct);
    }

    public async Task UpdateProductAsync(string id, ProductInput input, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"/products/{Uri.EscapeDataString(id)}", input, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteProductAsync(string id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"/products/{Uri.EscapeDataString(id)}", ct);
        response.EnsureSuccessStatusCode();
    }
}