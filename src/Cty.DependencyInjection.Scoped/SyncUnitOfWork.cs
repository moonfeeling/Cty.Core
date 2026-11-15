using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

/// <summary>
/// 同步作用域句柄。基于 IServiceScopeFactory.CreateScope() 创建，
/// 通过 using 释放。适用于无法走异步路径的场景。
/// </summary>
public sealed class SyncUnitOfWork : IDisposable
{
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _provider;
    private bool _disposed;

    internal SyncUnitOfWork(IServiceScope scope)
    {
        _scope = scope;
        _provider = scope.ServiceProvider;
    }

    public Guid ScopeId { get; } = Guid.NewGuid();

    public T GetRequiredService<T>() where T : class
    {
        ThrowIfDisposed();
        return _provider.GetRequiredService<T>();
    }

    public T? GetService<T>() where T : class
    {
        ThrowIfDisposed();
        return _provider.GetService<T>();
    }

    public IEnumerable<T> GetServices<T>() where T : class
    {
        ThrowIfDisposed();
        return _provider.GetServices<T>();
    }

    public T GetRequiredKeyedService<T>(object key) where T : class
    {
        ThrowIfDisposed();
        return _provider.GetRequiredKeyedService<T>(key);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SyncUnitOfWork),
                $"同步作用域 {ScopeId} 已释放，无法继续解析服务。");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _scope.Dispose();
    }
}