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
    /// 泛型便捷注册：为单个服务类型提供带 Scope 的解析工厂。
    /// </summary>
    public static IServiceCollection AddScopedResolver<TService>(
        this IServiceCollection services)
        where TService : class
    {
        services.AddSingleton<Func<ScopedResult<TService>>>(sp =>
            () =>
            {
                var scope = sp.CreateAsyncScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<TService>();
                return new ScopedResult<TService>(scope, service);
            });

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

    /// <summary>
    /// 泛型便捷注册：为单个服务类型提供带 Scope 的同步解析工厂。
    /// </summary>
    public static IServiceCollection AddSyncScopedResolver<TService>(
        this IServiceCollection services)
        where TService : class
    {
        services.AddSingleton<Func<SyncScopedResult<TService>>>(sp => () =>
        {
            var scope = sp.CreateScope();
            var service = scope.ServiceProvider
                .GetRequiredService<TService>();
            return new SyncScopedResult<TService>(scope, service);
        });

        return services;
    }
}