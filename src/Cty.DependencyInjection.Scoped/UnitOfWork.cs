using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

/// <summary>
/// 通用作用域句柄。封装一个独立的 DI Scope，
/// 通过 GetRequiredService / GetServices 按需解析任意服务。
/// 离开 using 时 Scope 及其持有的资源（DbContext、连接等）自动释放。
/// </summary>
public sealed class UnitOfWork : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;
    private readonly IServiceProvider _provider;
    private bool _disposed;

    internal UnitOfWork(AsyncServiceScope scope)
    {
        _scope = scope;
        _provider = scope.ServiceProvider;
    }

    /// <summary>内部作用域 ID，用于日志追踪。</summary>
    public Guid ScopeId { get; } = Guid.NewGuid();

    /// <summary>解析必需服务，缺失时抛出 InvalidOperationException。</summary>
    public T GetRequiredService<T>() where T : class
    {
        ThrowIfDisposed();
        return _provider.GetRequiredService<T>();
    }

    /// <summary>解析可选服务，缺失时返回 null。</summary>
    public T? GetService<T>() where T : class
    {
        ThrowIfDisposed();
        return _provider.GetService<T>();
    }

    /// <summary>解析全部已注册实现（对应 IEnumerable&lt;T&gt;）。</summary>
    public IEnumerable<T> GetServices<T>() where T : class
    {
        ThrowIfDisposed();
        return _provider.GetServices<T>();
    }

    /// <summary>按 Key 解析（.NET 8+ Keyed Services）。</summary>
    public T GetRequiredKeyedService<T>(object key) where T : class
    {
        ThrowIfDisposed();
        return _provider.GetRequiredKeyedService<T>(key);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWork),
                $"作用域 {ScopeId} 已释放，无法继续解析服务。");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _scope.DisposeAsync();
    }
}