# CODEBUDDY.md

This file provides guidance to CodeBuddy Code when working with code in this repository.

## 项目概述

Cty.Core 是一组 .NET 工具库的解决方案（`Cty.Core.slnx`，XML 格式解决方案文件），目前包含一个库：

- **Cty.DependencyInjection.Scoped** — 通用 DI 作用域管理库，解决 Singleton 捕获 Scoped 依赖（captive dependency）问题。提供 `UnitOfWork` 句柄与 `IServiceScopeExecutor` 模板执行器两种模式。

目标框架 net10.0，唯一外部依赖为 `Microsoft.Extensions.DependencyInjection`。代码注释与 XML 文档均为中文。

## 常用命令

```bash
# 构建（解决方案为 .slnx 格式）
dotnet build Cty.Core.slnx

# 运行全部测试（xUnit）
dotnet test Cty.Core.slnx

# 运行单个测试项目
dotnet test test/Cty.DependencyInjection.Scoped.Tests

# 运行单个测试（按类名或方法名过滤）
dotnet test test/Cty.DependencyInjection.Scoped.Tests --filter "FullyQualifiedName~UnitOfWorkTests.UnitOfWork_Should_Resolve_Scoped_Service"

# 打 NuGet 包（打包元数据内嵌于 csproj，含 snupkg 符号包）
dotnet pack src/Cty.DependencyInjection.Scoped
```

## 架构

库的所有类型都在单一命名空间 `Cty.DependencyInjection.Scoped` 下，分三层理解：

### 1. 执行器模式（推荐入口）

`IServiceScopeExecutor`（由 `ServiceScopeExecutor` 实现，注册为 Singleton，依赖注入 `IServiceScopeFactory`）：
- 每次调用创建一个全新 Scope，包装为 `UnitOfWork`/`SyncUnitOfWork` 传给业务委托
- 通过 try/finally 保证异常时 Scope 仍被释放
- 异步（`ExecuteAsync`）与同步（`Execute`）两组重载；`ExecuteAsync` 无返回值版本复用有返回值版本实现

### 2. 句柄模式（手动控制生命周期）

- `UnitOfWork`（异步，包 `AsyncServiceScope`，`IAsyncDisposable`）与 `SyncUnitOfWork`（同步，包 `IServiceScope`，`IDisposable`）互为镜像
- 均提供 `GetRequiredService`/`GetService`/`GetServices`/`GetRequiredKeyedService`（支持 Keyed Services），有 `ScopeId` 用于日志追踪
- 均在 Dispose 后再解析会抛 `ObjectDisposedException`（`_disposed` 标志守卫）

### 3. 泛型解析器模式（推荐用于解析特定服务）

`IScopedResolver<T>` / `ISyncScopedResolver<T>`（实现类 `ScopedResolver<T>` / `SyncScopedResolver<T>`，internal，只依赖 `IServiceScopeFactory`）：
- 通过 `AddScopedResolvers()` 以**开放泛型**一次性注册（类似 `ILogger<T>` 模式），无需逐服务类型注册
- 每次调用 `Resolve()` 创建全新 Scope 并解析服务，返回包装了 "scope + 已解析服务" 的 `ScopedResult<T>` / `SyncScopedResult<T>`
- 解析失败时实现内部保证已创建的 Scope 被释放（共用 `ScopeDisposal.DisposeOnFailure`：失败路径走 DisposeAsync + Task.Run，覆盖仅实现 IAsyncDisposable 的服务，且释放异常不掩盖原始解析异常）

### 4. 注册扩展（ScopedServiceCollectionExtensions）

- `AddScopedServiceExecutor()` — 注册执行器，主入口
- `AddUnitOfWorkFactory()` / `AddSyncUnitOfWorkFactory()` — 注册 `Func<UnitOfWork>` / `Func<SyncUnitOfWork>` 工厂委托，每次调用创建新 Scope
- `AddScopedResolvers()` — 开放泛型注册 `IScopedResolver<>` / `ISyncScopedResolver<>`，一次覆盖所有服务类型

### 设计约定

- 核心原则：Singleton 服务绝不直接捕获 Scoped 服务，要么走执行器模板，要么注入工厂委托在每次调用时新建 Scope
- `ScopedResult<TService>`/`SyncScopedResult<TService>` 构造函数为 internal，只能通过库内路径（`ScopedResolver<T>` 实现）创建
- 新增 API 时保持异步/同步两套表面对称（`UnitOfWork` ↔ `SyncUnitOfWork`、`ScopedResult` ↔ `SyncScopedResult`、`IScopedResolver<T>` ↔ `ISyncScopedResolver<T>`）
- 测试项目结构与测试内容镜像库本身（`test/Cty.DependencyInjection.Scoped.Tests`，测试类按被测类型命名）

## 新增库的约定

解决方案按 `src/`（库）+ `test/`（对应 `.Tests` 后缀测试项目）组织。添加新库时在 `Cty.Core.slnx` 的对应 Folder 节点中注册项目，命名遵循 `Cty.<类别>.<功能>`。
