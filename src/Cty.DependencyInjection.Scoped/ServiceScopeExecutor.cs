using Cty.DependencyInjection.Scoped;
using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

public sealed class ServiceScopeExecutor(
    IServiceScopeFactory scopeFactory) : IServiceScopeExecutor
{
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<UnitOfWork, Task<TResult>> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = scopeFactory.CreateAsyncScope();
        var uow = new UnitOfWork(scope);

        try
        {
            return await action(uow);
        }
        finally
        {
            // 确保即使 action 抛异常，Scope 也会被释放
            await uow.DisposeAsync();
        }
    }

    public async Task ExecuteAsync(
        Func<UnitOfWork, Task> action,
        CancellationToken ct = default)
    {
        await ExecuteAsync<object?>(async uow =>
        {
            await action(uow);
            return null;
        }, ct);
    }


    // ===== 同步 =====
    public TResult Execute<TResult>(
        Func<SyncUnitOfWork, TResult> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var scope = scopeFactory.CreateScope();
        var uow = new SyncUnitOfWork(scope);

        try
        {
            return action(uow);
        }
        finally
        {
            uow.Dispose();
        }
    }

    public void Execute(Action<SyncUnitOfWork> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var scope = scopeFactory.CreateScope();
        var uow = new SyncUnitOfWork(scope);

        try
        {
            action(uow);
        }
        finally
        {
            uow.Dispose();
        }
    }
}