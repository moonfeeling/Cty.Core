namespace Cty.DependencyInjection.Scoped;

/// <summary>
/// 同步泛型作用域解析器，<see cref="IScopedResolver{TService}"/> 的同步镜像。
/// 仅在确实无法使用异步路径时使用。
/// 通过 <see cref="ScopedServiceCollectionExtensions.AddScopedResolvers"/>
/// 以开放泛型方式注册，一次注册即可覆盖所有服务类型。
/// </summary>
public interface ISyncScopedResolver<TService> where TService : class
{
    /// <summary>
    /// 创建新 Scope 并解析服务。
    /// 调用方负责释放返回的 <see cref="SyncScopedResult{TService}"/>。
    /// </summary>
    SyncScopedResult<TService> Resolve();
}
