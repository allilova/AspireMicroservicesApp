using System.Threading.Channels;

namespace Shop.OrdersApi.Notifications;

public record OrderNotification(string Type, string Message, string? OrderId);

public class NotificationQueue
{
    // ограничена опашка: ако е пълна, най-старите известия се изхвърлят, и записът никога не блокира
    private readonly Channel<OrderNotification> _channel = Channel.CreateBounded<OrderNotification>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });

    public bool TryEnqueue(OrderNotification notification) => _channel.Writer.TryWrite(notification);

    public IAsyncEnumerable<OrderNotification> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}