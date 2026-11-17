namespace Cty.DependencyInjection.Scoped;

/// <summary>
/// 泛型作用域解析器：每次调用 <see cref="Resolve"/> 创建一个全新 Scope，
/// 并在其中解析 <typeparamref name="TService"/>，返回包装了 Scope 的
/// <see cref="ScopedResult{TService}"/>。
/// 通过 <see cref="ScopedServiceCollectionExtensions.AddScopedResolvers"/>
/// 以开放泛型方式注册，一次注册即可覆盖所有服务类型。
/// </summary>
public interface IScopedResolver<TService> where TService : class
{
    /// <summary>
    /// 创建新 Scope 并解析服务。
    /// 调用方负责释放返回的 <see cref="ScopedResult{TService}"/>。
    /// </summary>
    ScopedResult<TService> Resolve();
}
