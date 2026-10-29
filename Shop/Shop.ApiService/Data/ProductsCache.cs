using Microsoft.Extensions.Primitives;

namespace Shop.CatalogApi.Data
{
    public class ProductsCache
    {
        private CancellationTokenSource _cts = new();

        public CancellationChangeToken GetToken() => new(_cts.Token);

        public void Invalidate()
        {
            var old = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            old.Cancel();
            old.Dispose();
        }
    }
}
