using System.Net;

namespace Shop.Web;

public class OrdersApiClient(HttpClient http)
{
    public async Task<List<OrderDto>> GetOrdersAsync(string? userId = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(userId) ? "/orders" : $"/orders?userId={Uri.EscapeDataString(userId)}";
        return await http.GetFromJsonAsync<List<OrderDto>>(url, ct) ?? [];
    }
    public async Task<(OrderDto? Order, string? Error)> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/orders", request, ct);

        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<OrderDto>(ct), null);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            return (null, "Услугата е временно недостъпна. Опитайте по-късно.");

        string? message;
        try
        {
            message = await response.Content.ReadFromJsonAsync<string>(ct);
        }
        catch (System.Text.Json.JsonException)
        {
            message = await response.Content.ReadAsStringAsync(ct);
        }
        return (null, message ?? "Грешка при създаване на поръчката.");
    }
}