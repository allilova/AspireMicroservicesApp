namespace Shop.Web;

public record ProductDto(string Id, string Name, string Category, string? Description, decimal Price, int Stock, string? ImageUrl = null);
public record OrderItemDto(string ProductId, string ProductName, decimal UnitPrice, int Quantity);
public record OrderDto(string Id, string CustomerName, string Status, DateTime CreatedAt, List<OrderItemDto> Items, decimal Total);

public record CreateOrderLine(string ProductId, int Quantity);
public record CreateOrderRequest(string CustomerName, List<CreateOrderLine> Items);