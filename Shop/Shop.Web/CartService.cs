namespace Shop.Web;

public class CartLine(ProductDto product, int quantity)
{
    public ProductDto Product { get; } = product;
    public int Quantity { get; set; } = quantity;
}

public class CartService
{
    private readonly List<CartLine> _lines = [];

    public IReadOnlyList<CartLine> Lines => _lines;
    public int Count => _lines.Sum(l => l.Quantity);
    public decimal Total => _lines.Sum(l => l.Product.Price * l.Quantity);

    public void Add(ProductDto product)
    {
        var line = _lines.FirstOrDefault(l => l.Product.Id == product.Id);
        if (line is null) _lines.Add(new CartLine(product, 1));
        else line.Quantity++;
    }

    public void Remove(string productId) => _lines.RemoveAll(l => l.Product.Id == productId);

    public void Clear() => _lines.Clear();
}