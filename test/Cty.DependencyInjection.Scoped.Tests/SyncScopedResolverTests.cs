using Cty.DependencyInjection.Scoped;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cty.DependencyInjection.Scoped.Tests;

public class SyncScopedResolverTests
{
    private interface IRepo { string Load(); }
    private class Repo : IRepo { public string Load() => "sync-data"; }

    private interface IEmail { void Send(); }
    private class Email : IEmail { public void Send() { } }

    private class DisposeTracker
    {
        public int DisposedCount;
    }

    // 仅实现 IAsyncDisposable 的服务（同步 Dispose 路径无法释放的类型）
    private class AsyncOnly : IAsyncDisposable
    {
        private readonly DisposeTracker _tracker;

        public AsyncOnly(DisposeTracker tracker) => _tracker = tracker;

        public ValueTask DisposeAsync()
        {
            _tracker.DisposedCount++;
            return ValueTask.CompletedTask;
        }
    }

    private class ThrowingAsyncOnly : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => throw new InvalidOperationException("dispose failed");
    }

    private class Broken
    {
        public Broken(AsyncOnly dep) => throw new InvalidOperationException("construct failed");
    }

    private class BrokenWithThrowingDispose
    {
        public BrokenWithThrowingDispose(ThrowingAsyncOnly dep)
            => throw new InvalidOperationException("construct failed 2");
    }

    private static IServiceProvider BuildProvider()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IRepo, Repo>();
        sc.AddScoped<IEmail, Email>();
        sc.AddScopedResolvers();
        return sc.BuildServiceProvider();
    }

    [Fact]
    public void Single_Registration_Should_Resolve_Multiple_Service_Types()
    {
        var sp = BuildProvider();

        var repoResolver = sp.GetRequiredService<ISyncScopedResolver<IRepo>>();
        var emailResolver = sp.GetRequiredService<ISyncScopedResolver<IEmail>>();

        using var repoResult = repoResolver.Resolve();
        using var emailResult = emailResolver.Resolve();

        Assert.Equal("sync-data", repoResult.Service.Load());
        Assert.IsAssignableFrom<IEmail>(emailResult.Service);
    }

    [Fact]
    public void Resolve_Should_Create_New_Scope_Each_Call()
    {
        var sp = BuildProvider();
        var resolver = sp.GetRequiredService<ISyncScopedResolver<IRepo>>();

        using var r1 = resolver.Resolve();
        using var r2 = resolver.Resolve();

        Assert.NotSame(r1.Service, r2.Service);
    }

    [Fact]
    public void Resolve_Unregistered_Service_Should_Throw()
    {
        var sc = new ServiceCollection();
        sc.AddScopedResolvers();
        var sp = sc.BuildServiceProvider();

        var resolver = sp.GetRequiredService<ISyncScopedResolver<IRepo>>();

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve());
    }

    [Fact]
    public void Sync_And_Async_Resolvers_Should_Be_Independent()
    {
        var sp = BuildProvider();

        var syncResolver = sp.GetRequiredService<ISyncScopedResolver<IRepo>>();
        var asyncResolver = sp.GetRequiredService<IScopedResolver<IRepo>>();

        using var syncResult = syncResolver.Resolve();
        Assert.Equal("sync-data", syncResult.Service.Load());

        // 异步版本在同一容器中同样可用
        var asyncResult = asyncResolver.Resolve();
        Assert.Equal("sync-data", asyncResult.Service.Load());
        asyncResult.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Fact]
    public void Resolve_Failure_Should_Dispose_AsyncOnly_Services_And_Preserve_Original_Exception()
    {
        var tracker = new DisposeTracker();
        var sc = new ServiceCollection();
        sc.AddSingleton(tracker);
        sc.AddScoped<AsyncOnly>();
        sc.AddScoped<Broken>();
        sc.AddScopedResolvers();
        var sp = sc.BuildServiceProvider();

        var resolver = sp.GetRequiredService<ISyncScopedResolver<Broken>>();

        var ex = Record.Exception(() => resolver.Resolve());

        // 调用方看到原始构造异常，而非容器释放路径的异常
        Assert.Equal("construct failed", ex?.Message);
        // 仅实现 IAsyncDisposable 的半成品服务也被正确释放
        Assert.Equal(1, tracker.DisposedCount);
    }

    [Fact]
    public void Resolve_Failure_Should_Not_Mask_Original_Exception_When_Disposal_Throws()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ThrowingAsyncOnly>();
        sc.AddScoped<BrokenWithThrowingDispose>();
        sc.AddScopedResolvers();
        var sp = sc.BuildServiceProvider();

        var resolver = sp.GetRequiredService<ISyncScopedResolver<BrokenWithThrowingDispose>>();

        var ex = Record.Exception(() => resolver.Resolve());

        Assert.Equal("construct failed 2", ex?.Message);
    }
}
