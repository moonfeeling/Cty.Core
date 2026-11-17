using Microsoft.Extensions.DependencyInjection;

namespace Cty.DependencyInjection.Scoped;

/// <summary>
/// 解析失败路径的 Scope 清理逻辑（ScopedResolver / SyncScopedResolver 共用）。
/// </summary>
internal static class ScopeDisposal
{
    /// <summary>
    /// 释放解析失败的 Scope。
    /// 走 DisposeAsync 以覆盖仅实现 IAsyncDisposable 的服务——同步 Dispose 对这类服务
    /// 既不会释放、还会抛 InvalidOperationException；通过 Task.Run 在无线程池上下文
    /// 中执行，规避 SynchronizationContext 死锁（与 MS DI 容器内部 CaptureDisposable
    /// 的做法一致）。
    /// </summary>
    internal static void DisposeOnFailure(IServiceScope scope)
    {
        try
        {
            var asyncScope = new AsyncServiceScope(scope);
            Task.Run(() => asyncScope.DisposeAsync().AsTask()).GetAwaiter().GetResult();
        }
        catch
        {
            // 释放失败不应掩盖导致解析失败的原始异常
        }
    }
}
