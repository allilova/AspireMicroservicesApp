using Shop.Web;

namespace Shop.Tests;

public class CartServiceTests
{
    private static ProductDto Product(int id, decimal price) =>
      new(id.ToString(), $"Продукт {id}", "Тест", null, price);

    [Fact]
    public void Adding_same_product_twice_increases_quantity()
    {
        var cart = new CartService();
        var p = Product(1, 10m);

        cart.Add(p);
        cart.Add(p);

        Assert.Single(cart.Lines);
        Assert.Equal(2, cart.Lines[0].Quantity);
        Assert.Equal(2, cart.Count);
    }

    [Fact]
    public void Total_is_sum_of_price_times_quantity()
    {
        var cart = new CartService();
        cart.Add(Product(1, 10m));
        cart.Add(Product(1, 10m));
        cart.Add(Product(2, 5.50m));

        Assert.Equal(25.50m, cart.Total);
    }

    [Fact]
    public void Remove_deletes_only_that_product()
    {
        var cart = new CartService();
        cart.Add(Product(1, 10m));
        cart.Add(Product(2, 5m));

        cart.Remove("1");

        Assert.Single(cart.Lines);
        Assert.Equal("2", cart.Lines[0].Product.Id);
    }

    [Fact]
    public void Clear_empties_the_cart()
    {
        var cart = new CartService();
        cart.Add(Product(1, 10m));

        cart.Clear();

        Assert.Empty(cart.Lines);
        Assert.Equal(0, cart.Total);
    }
}