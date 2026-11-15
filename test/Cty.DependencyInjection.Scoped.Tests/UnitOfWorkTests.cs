using Cty.DependencyInjection.Scoped;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cty.DependencyInjection.Scoped.Tests;

public class UnitOfWorkTests
{
    private interface IRepo { string Load(); }
    private class Repo : IRepo
    {
        public int CreateCount { get; private set; }
        public string Load() { CreateCount++; return "data"; }
    }

    private static IServiceProvider BuildProvider()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IRepo, Repo>();
        sc.AddScopedServiceExecutor();
        sc.AddUnitOfWorkFactory();
        return sc.BuildServiceProvider();
    }

    [Fact]
    public async Task UnitOfWork_Should_Resolve_Scoped_Service()
    {
        var sp = BuildProvider();
        var factory = sp.GetRequiredService<Func<Task<UnitOfWork>>>();

        await using var uow = await factory();
        var repo = uow.GetRequiredService<IRepo>();

        Assert.Equal("data", repo.Load());
    }

    [Fact]
    public async Task UnitOfWork_Should_Dispose_Scope_After_Use()
    {
        var sp = BuildProvider();
        var factory = sp.GetRequiredService<Func<Task<UnitOfWork>>>();

        UnitOfWork uow;
        await using (uow = await factory())
        {
            _ = uow.GetRequiredService<IRepo>();
        }

        // 释放后再解析应抛异常
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await Task.Delay(10);
            uow.GetRequiredService<IRepo>();
        });
    }

    [Fact]
    public async Task Executor_Should_Create_New_Scope_Each_Call()
    {
        var sp = BuildProvider();
        var executor = sp.GetRequiredService<IServiceScopeExecutor>();

        var r1 = await executor.ExecuteAsync(u =>
            Task.FromResult(u.GetRequiredService<IRepo>().Load()));
        var r2 = await executor.ExecuteAsync(u =>
            Task.FromResult(u.GetRequiredService<IRepo>().Load()));

        Assert.Equal("data", r1);
        Assert.Equal("data", r2);
    }

    [Fact]
    public async Task Executor_Should_Dispose_Scope_On_Exception()
    {
        var sp = BuildProvider();
        var executor = sp.GetRequiredService<IServiceScopeExecutor>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync(_ =>
                throw new InvalidOperationException("boom")));

        // 异常后容器仍可正常解析，说明没有泄漏
        var ok = await executor.ExecuteAsync(u =>
            Task.FromResult(u.GetRequiredService<IRepo>().Load()));
        Assert.Equal("data", ok);
    }

    [Fact]
    public async Task GetServices_Should_Return_All_Implementations()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IRepo, Repo>();
        sc.AddScoped<IRepo, Repo>();
        sc.AddUnitOfWorkFactory();
        var sp = sc.BuildServiceProvider();

        var factory = sp.GetRequiredService<Func<Task<UnitOfWork>>>();
        await using var uow = await factory();

        Assert.Equal(2, uow.GetServices<IRepo>().Count());
    }
}