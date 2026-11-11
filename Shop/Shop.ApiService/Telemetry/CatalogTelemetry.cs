using System.Diagnostics.Metrics;

namespace Shop.CatalogApi.Telemetry;

public static class CatalogTelemetry
{
    public const string Name = "Shop.Catalog";

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> Reservations =
        Meter.CreateCounter<long>("shop.catalog.reservations", unit: "{reservation}", description: "Опити за резервиране на наличност, по резултат");
}