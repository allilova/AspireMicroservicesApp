namespace Shop.Web;

public class CartLine(ProductDto product, int quantity)
{
    public ProductDto Product { get; } = product;
    public int Quantity { get; set; } = quantity;
}

public class CartService
{
    private readonly List<CartLine> _lines = [];

    public event Action? Changed;

    public IReadOnlyList<CartLine> Lines => _lines;
    public int Count => _lines.Sum(l => l.Quantity);
    public decimal Total => _lines.Sum(l => l.Product.Price * l.Quantity);

    // връща false, ако количеството би надвишило наличността
    public bool Add(ProductDto product)
    {
        var line = _lines.FirstOrDefault(l => l.Product.Id == product.Id);
        if (line is null)
        {
            if (product.Stock < 1) return false;
            _lines.Add(new CartLine(product, 1));
        }
        else
        {
            if (line.Quantity >= product.Stock) return false;
            line.Quantity++;
        }
        Changed?.Invoke();
        return true;
    }

    public void Decrease(string productId)
    {
        var line = _lines.FirstOrDefault(l => l.Product.Id == productId);
        if (line is null) return;
        line.Quantity--;
        if (line.Quantity <= 0) _lines.Remove(line);
        Changed?.Invoke();
    }

    public void Remove(string productId)
    {
        _lines.RemoveAll(l => l.Product.Id == productId);
        Changed?.Invoke();
    }

    public void Clear()
    {
        _lines.Clear();
        Changed?.Invoke();
    }
}