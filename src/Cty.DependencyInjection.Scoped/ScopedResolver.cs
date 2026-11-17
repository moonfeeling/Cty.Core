using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

internal sealed class ScopedResolver<TService>(
    IServiceScopeFactory scopeFactory) : IScopedResolver<TService>
    where TService : class
{
    public ScopedResult<TService> Resolve()
    {
        var scope = scopeFactory.CreateAsyncScope();

        try
        {
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            return new ScopedResult<TService>(scope, service);
        }
        catch
        {
            // 解析失败时释放已创建的 Scope，避免半成品依赖图泄漏
            ScopeDisposal.DisposeOnFailure(scope);
            throw;
        }
    }
}
