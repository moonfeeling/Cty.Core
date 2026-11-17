using Cty.DependencyInjection.Scoped;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cty.DependencyInjection.Scoped.Tests;

public class ScopedResolverTests
{
    private interface IRepo { string Load(); }
    private class Repo : IRepo { public string Load() => "data"; }

    private interface IEmail { void Send(); }
    private class Email : IEmail { public void Send() { } }

    private static IServiceProvider BuildProvider()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IRepo, Repo>();
        sc.AddScoped<IEmail, Email>();
        sc.AddScopedResolvers();
        return sc.BuildServiceProvider();
    }

    [Fact]
    public async Task Single_Registration_Should_Resolve_Multiple_Service_Types()
    {
        var sp = BuildProvider();

        // 一次 AddScopedResolvers 即可解析任意已注册类型，无需逐类型注册
        var repoResolver = sp.GetRequiredService<IScopedResolver<IRepo>>();
        var emailResolver = sp.GetRequiredService<IScopedResolver<IEmail>>();

        await using var repoResult = repoResolver.Resolve();
        await using var emailResult = emailResolver.Resolve();

        Assert.Equal("data", repoResult.Service.Load());
        Assert.IsAssignableFrom<IEmail>(emailResult.Service);
    }

    [Fact]
    public async Task Resolve_Should_Create_New_Scope_Each_Call()
    {
        var sp = BuildProvider();
        var resolver = sp.GetRequiredService<IScopedResolver<IRepo>>();

        await using var r1 = resolver.Resolve();
        await using var r2 = resolver.Resolve();

        // Scoped 服务在不同 Scope 中应是不同实例
        Assert.NotSame(r1.Service, r2.Service);
    }

    [Fact]
    public async Task Resolve_Unregistered_Service_Should_Throw()
    {
        var sc = new ServiceCollection();
        sc.AddScopedResolvers();
        var sp = sc.BuildServiceProvider();

        var resolver = sp.GetRequiredService<IScopedResolver<IRepo>>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.Run(() => resolver.Resolve()));

        // 异常路径没有泄漏 Scope，容器仍可正常工作
        var sp2 = BuildProvider();
        var ok = sp2.GetRequiredService<IScopedResolver<IRepo>>();
        await using var result = ok.Resolve();
        Assert.Equal("data", result.Service.Load());
    }

    [Fact]
    public async Task Resolver_Should_Be_Singleton()
    {
        var sp = BuildProvider();

        var r1 = sp.GetRequiredService<IScopedResolver<IRepo>>();
        var r2 = sp.GetRequiredService<IScopedResolver<IRepo>>();

        Assert.Same(r1, r2);
    }
}
