using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

/// <summary>泛型解析结果包装。</summary>
public sealed class ScopedResult<TService> : IAsyncDisposable
    where TService : class
{
    private readonly AsyncServiceScope _scope;

    internal ScopedResult(AsyncServiceScope scope, TService service)
    {
        _scope = scope;
        Service = service;
    }

    public TService Service { get; }

    public ValueTask DisposeAsync() => _scope.DisposeAsync();
}