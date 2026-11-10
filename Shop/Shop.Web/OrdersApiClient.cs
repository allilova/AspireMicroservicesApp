using System.Net;

namespace Shop.Web;

public class OrdersApiClient(HttpClient http)
{
    public async Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<OrderDto>>("/orders", ct) ?? [];

    public async Task<(OrderDto? Order, string? Error)> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/orders", request, ct);

        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<OrderDto>(ct), null);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            return (null, "Каталогът не е достъпен. Опитайте по-късно.");

        var text = await response.Content.ReadAsStringAsync(ct);
        return (null, text.Trim('"'));
    }
}