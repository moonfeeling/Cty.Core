using Cty.DependencyInjection.Scoped;

namespace Cty.DependencyInjection.Scoped;

/// <summary>
/// 作用域执行器：把 Scope 的创建与释放完全隐藏，
/// 调用方只需提供业务委托。
/// </summary>
public interface IServiceScopeExecutor
{
    // ===== 异步 =====
    Task<TResult> ExecuteAsync<TResult>(
        Func<UnitOfWork, Task<TResult>> action,
        CancellationToken ct = default);

    Task ExecuteAsync(
        Func<UnitOfWork, Task> action,
        CancellationToken ct = default);

    // ===== 同步 =====
    TResult Execute<TResult>(
        Func<SyncUnitOfWork, TResult> action);

    void Execute(
        Action<SyncUnitOfWork> action);
}