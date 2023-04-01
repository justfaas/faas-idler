using Microsoft.Extensions.Caching.Memory;

internal sealed class FunctionStateStore
{
    private readonly IMemoryCache cache;
    private readonly MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromSeconds( 20 )
    };

    public FunctionStateStore( IMemoryCache memoryCache )
    {
        cache = memoryCache;
    }

    public FunctionState Get( string key )
        => cache.Get<FunctionState>( key ) ?? new FunctionState();

    public void Set( string key, FunctionState value )
        => cache.Set<FunctionState>( key, value, cacheEntryOptions );
}
