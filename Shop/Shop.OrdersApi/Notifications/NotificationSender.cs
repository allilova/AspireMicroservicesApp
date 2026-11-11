namespace Shop.OrdersApi.Notifications;

public class NotificationSender(NotificationQueue queue, IHttpClientFactory httpFactory, ILogger<NotificationSender> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10));

                var http = httpFactory.CreateClient("notifications");
                var response = await http.PostAsJsonAsync("/notifications", notification, cts.Token);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // не прекъсваме работата: просто записваме предупреждение
                logger.LogWarning(ex, "Известието „{Type}“ не беше изпратено (услугата за известия не е достъпна)", notification.Type);
            }
        }
    }
}