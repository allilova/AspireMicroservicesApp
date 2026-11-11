namespace Shop.Web;

public class NotificationsApiClient(HttpClient http)
{
    public async Task<List<NotificationDto>> GetAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<NotificationDto>>("/notifications", ct) ?? [];
}