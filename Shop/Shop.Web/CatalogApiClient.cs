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
}