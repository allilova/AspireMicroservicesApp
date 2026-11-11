using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shop.OrdersWorker;

public static class WorkerTelemetry
{
    public const string Name = "Shop.Worker";

    public static readonly ActivitySource Source = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> OrdersAdvanced =
        Meter.CreateCounter<long>("shop.worker.orders_advanced", unit: "{order}", description: "Поръчки, придвижени към нов статус, по статус");
}