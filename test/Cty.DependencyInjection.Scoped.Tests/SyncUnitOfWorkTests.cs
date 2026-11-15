using Cty.DependencyInjection.Scoped;
using Microsoft.Extensions.DependencyInjection;
using Cty.DependencyInjection.Scoped;
using Xunit;

namespace Cty.DependencyInjection.Scoped.Tests;

public class SyncUnitOfWorkTests
{
    private interface IRepo { string Load(); }
    private class Repo : IRepo
    {
        public string Load() => "sync-data";
    }

    private static IServiceProvider BuildProvider()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IRepo, Repo>();
        sc.AddScopedServiceExecutor();
        sc.AddSyncUnitOfWorkFactory();
        return sc.BuildServiceProvider();
    }

    [Fact]
    public void Sync_Executor_Should_Resolve_Service()
    {
        var sp = BuildProvider();
        var executor = sp.GetRequiredService<IServiceScopeExecutor>();

        var result = executor.Execute(u =>
            u.GetRequiredService<IRepo>().Load());

        Assert.Equal("sync-data", result);
    }

    [Fact]
    public void Sync_UnitOfWork_Should_Throw_After_Dispose()
    {
        var sp = BuildProvider();
        var factory = sp.GetRequiredService<Func<SyncUnitOfWork>>();

        SyncUnitOfWork uow;
        using (uow = factory())
        {
            _ = uow.GetRequiredService<IRepo>();
        }

        Assert.Throws<ObjectDisposedException>(() =>
            uow.GetRequiredService<IRepo>());
    }

    [Fact]
    public void Sync_Executor_Should_Dispose_Scope_On_Exception()
    {
        var sp = BuildProvider();
        var executor = sp.GetRequiredService<IServiceScopeExecutor>();

        Assert.Throws<InvalidOperationException>(() =>
            executor.Execute(_ => throw new InvalidOperationException("boom")));

        // 异常后仍可正常解析
        var ok = executor.Execute(u =>
            u.GetRequiredService<IRepo>().Load());
        Assert.Equal("sync-data", ok);
    }

    [Fact]
    public void Sync_And_Async_Should_Use_Independent_Scopes()
    {
        var sp = BuildProvider();
        var executor = sp.GetRequiredService<IServiceScopeExecutor>();

        var syncResult = executor.Execute(u =>
            u.GetRequiredService<IRepo>().Load());

        var asyncResult = executor.ExecuteAsync(u =>
            Task.FromResult(u.GetRequiredService<IRepo>().Load()))
            .GetAwaiter().GetResult();

        Assert.Equal("sync-data", syncResult);
        Assert.Equal("sync-data", asyncResult);
    }
}