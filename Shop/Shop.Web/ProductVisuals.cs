using MudBlazor;

namespace Shop.Web;

public static class ProductVisuals
{
    private static readonly string[] Gradients =
    [
        "linear-gradient(135deg,#667eea,#764ba2)",
        "linear-gradient(135deg,#11998e,#38ef7d)",
        "linear-gradient(135deg,#f7971e,#ffd200)",
        "linear-gradient(135deg,#ee0979,#ff6a00)",
        "linear-gradient(135deg,#2193b0,#6dd5ed)"
    ];

    public static string GradientFor(string category) => Gradients[category.Sum(ch => ch) % Gradients.Length];

    public static string IconFor(string category) => category switch
    {
        "Компютри" => Icons.Material.Filled.Computer,
        "Аксесоари" => Icons.Material.Filled.Headphones,
        _ => Icons.Material.Filled.Inventory2
    };
}