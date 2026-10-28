using Microsoft.EntityFrameworkCore;

namespace Shop.CatalogApi.Data
{
    public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
    }
}
