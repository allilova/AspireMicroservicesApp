using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shop.OrdersApi.Telemetry;

public static class OrdersTelemetry
{
    public const string Name = "Shop.Orders";

    // източник на трасета (span-ове)
    public static readonly ActivitySource Source = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> OrdersCreated =
        Meter.CreateCounter<long>("shop.orders.created", unit: "{order}", description: "Брой успешно създадени поръчки");

    public static readonly Counter<long> OrdersRejected =
        Meter.CreateCounter<long>("shop.orders.rejected", unit: "{order}", description: "Брой отказани поръчки, по причина");

    public static readonly Counter<long> StockCompensations =
        Meter.CreateCounter<long>("shop.orders.stock_compensations", unit: "{compensation}", description: "Колко пъти е връщана резервирана наличност");

    public static readonly Histogram<double> OrderValue =
        Meter.CreateHistogram<double>("shop.orders.value", unit: "EUR", description: "Стойност на създадените поръчки");
}