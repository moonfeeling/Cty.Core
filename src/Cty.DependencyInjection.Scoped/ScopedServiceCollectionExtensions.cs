using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

/// <summary>
/// 
/// </summary>
public static class ScopedServiceCollectionExtensions
{
    /// <summary>
    /// 注册作用域执行器（Singleton）。
    /// 这是推荐的主入口。
    /// </summary>
    public static IServiceCollection AddScopedServiceExecutor(
        this IServiceCollection services)
    {
        services.AddSingleton<IServiceScopeExecutor, ServiceScopeExecutor>();
        return services;
    }

    /// <summary>
    /// 注册通用 UnitOfWork 工厂（Singleton）。
    /// 适合需要手动控制 Scope 生命周期的场景。
    /// </summary>
    public static IServiceCollection AddUnitOfWorkFactory(
        this IServiceCollection services)
    {
        services.AddSingleton<Func<UnitOfWork>>(sp => () =>
        {
            var scope = sp.CreateAsyncScope();
            return new UnitOfWork(scope);
        });

        return services;
    }

    /// <summary>
    /// 以开放泛型方式一次性注册所有服务类型的
    /// <see cref="IScopedResolver{TService}"/> 与 <see cref="ISyncScopedResolver{TService}"/>。
    /// 无需为每个服务类型单独调用注册方法（类似 ILogger&lt;T&gt; 的模式）。
    /// </summary>
    public static IServiceCollection AddScopedResolvers(
        this IServiceCollection services)
    {
        services.AddSingleton(typeof(IScopedResolver<>), typeof(ScopedResolver<>));
        services.AddSingleton(typeof(ISyncScopedResolver<>), typeof(SyncScopedResolver<>));

        return services;
    }


    /// <summary>
    /// 注册同步 UnitOfWork 工厂（Singleton）。
    /// 仅在确实无法使用异步路径时使用。
    /// </summary>
    public static IServiceCollection AddSyncUnitOfWorkFactory(
        this IServiceCollection services)
    {
        services.AddSingleton<Func<SyncUnitOfWork>>(sp => () =>
        {
            var scope = sp.CreateScope();
            return new SyncUnitOfWork(scope);
        });

        return services;
    }
}