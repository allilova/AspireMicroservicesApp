using System.Diagnostics.Metrics;
using Shop.OrdersApi.Telemetry;

namespace Shop.Tests;

public class TelemetryTests
{
    [Fact]
    public void Orders_created_counter_records_measurements()
    {
        long total = 0;

        // слушател, който улавя измерванията на брояча
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == OrdersTelemetry.Name && instrument.Name == "shop.orders.created")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) => total += value);
        listener.Start();

        OrdersTelemetry.OrdersCreated.Add(1);
        OrdersTelemetry.OrdersCreated.Add(2);

        Assert.Equal(3, total);
    }
}