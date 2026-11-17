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
            // 解析失败时同样要释放已创建的 Scope，避免泄漏
            scope.Dispose();
            throw;
        }
    }
}
