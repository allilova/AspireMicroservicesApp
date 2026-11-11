using Shop.OrdersWorker;

namespace Shop.Tests;

public class OrderProgressionTests
{
    private static readonly WorkerOptions Options = new() { ConfirmAfterSeconds = 15, ShipAfterSeconds = 40 };

    [Theory]
    [InlineData("New", 5, null)]
    [InlineData("New", 20, "Confirmed")]
    [InlineData("Confirmed", 20, null)]
    [InlineData("Confirmed", 50, "Shipped")]
    [InlineData("Shipped", 500, null)]
    [InlineData("Cancelled", 500, null)]
    public void NextStatus_follows_the_rules(string status, int ageSeconds, string? expected)
    {
        var next = OrderProgression.NextStatus(status, TimeSpan.FromSeconds(ageSeconds), Options);

        Assert.Equal(expected, next);
    }
}