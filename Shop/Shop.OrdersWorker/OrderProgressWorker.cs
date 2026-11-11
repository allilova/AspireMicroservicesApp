using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Shop.OrdersWorker;

public class OrderProgressWorker(
    IHttpClientFactory httpFactory,
    IOptions<WorkerOptions> options,
    ILogger<OrderProgressWorker> logger) : BackgroundService
{
    private record OrderDto(string Id, string Status, DateTime CreatedAt);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.Value;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(opt.PollSeconds));

        do
        {
            try
            {
                await ProcessAsync(opt, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // услугата за поръчки може временно да не е достъпна: опитваме пак в следващия цикъл
                logger.LogWarning(ex, "Неуспешен цикъл на обработка, ще опитам отново");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(WorkerOptions opt, CancellationToken ct)
    {
        var http = httpFactory.CreateClient("orders");

        foreach (var status in new[] { "New", "Confirmed" })
        {
            var orders = await http.GetFromJsonAsync<List<OrderDto>>($"/orders?status={status}", ct) ?? [];

            foreach (var order in orders)
            {
                var age = DateTime.UtcNow - order.CreatedAt.ToUniversalTime();
                var next = OrderProgression.NextStatus(order.Status, age, opt);
                if (next is null) continue;

                using var activity = WorkerTelemetry.Source.StartActivity("AdvanceOrder");
                activity?.SetTag("order.id", order.Id);
                activity?.SetTag("order.from", order.Status);
                activity?.SetTag("order.to", next);

                var response = await http.PutAsJsonAsync($"/orders/{order.Id}/status", new { status = next }, ct);
                if (response.IsSuccessStatusCode)
                {
                    WorkerTelemetry.OrdersAdvanced.Add(1, new KeyValuePair<string, object?>("to_status", next));
                    logger.LogInformation("Поръчка {OrderId}: {From} -> {To}", order.Id, order.Status, next);
                }
                else
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    logger.LogWarning("Промяната на поръчка {OrderId} към {To} върна {Status}", order.Id, next, (int)response.StatusCode);
                }
            }
        }
    }
}
