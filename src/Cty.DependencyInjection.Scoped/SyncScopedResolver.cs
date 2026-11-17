using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

internal sealed class SyncScopedResolver<TService>(
    IServiceScopeFactory scopeFactory) : ISyncScopedResolver<TService>
    where TService : class
{
    public SyncScopedResult<TService> Resolve()
    {
        var scope = scopeFactory.CreateScope();

        try
        {
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            return new SyncScopedResult<TService>(scope, service);
        }
        catch
        {
            // 解析失败时释放已创建的 Scope，避免半成品依赖图泄漏
            ScopeDisposal.DisposeOnFailure(scope);
            throw;
        }
    }
}
