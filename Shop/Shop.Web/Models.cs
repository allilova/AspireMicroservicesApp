namespace Shop.Web;

public record ProductDto(int Id, string Name, string Category, string? Description, decimal Price);

public record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
public record OrderDto(int Id, string CustomerName, string Status, DateTime CreatedAt, List<OrderItemDto> Items, decimal Total);

public record CreateOrderLine(int ProductId, int Quantity);
public record CreateOrderRequest(string CustomerName, List<CreateOrderLine> Items);