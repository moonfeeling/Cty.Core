using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

/// <summary>同步泛型解析结果包装。</summary>
public sealed class SyncScopedResult<TService> : IDisposable
    where TService : class
{
    private readonly IServiceScope _scope;

    internal SyncScopedResult(IServiceScope scope, TService service)
    {
        _scope = scope;
        Service = service;
    }

    public TService Service { get; }

    public void Dispose() => _scope.Dispose();
}