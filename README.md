# MiniGin - .NET 轻量级 HTTP 框架

🚀 基于 **高性能 Socket** 的轻量级 HTTP 框架，借鉴 Go Gin 的优雅 API 风格，采用面向对象设计。

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

## ✨ 核心特性

- 🔥 **高性能 Socket** - 原生 Socket 实现，支持 HTTP/1.1 Keep-Alive
- 🎯 **Gin 风格 API** - 熟悉的链式路由、中间件管道
- 🔌 **依赖注入** - 内置轻量级 DI 容器
- ⏰ **后台服务** - 支持托管服务和定时任务
- 🗄️ **数据库支持** - MySQL/SQLite/SQL Server/PostgreSQL
- 🎮 **控制器模式** - 类似 ASP.NET Core 的控制器写法
- 📚 **Swagger 集成** - 自动生成 OpenAPI 文档
- 🛡️ **丰富中间件** - Logger、CORS、Auth、Static 等

## 🏗️ 架构说明

MiniGin 使用原生 Socket 实现 HTTP 服务器，相比 HttpListener：

| 特性 | MiniGin Socket | HttpListener |
|------|---------------|--------------|
| 内核模式 | 用户态 Socket | 内核态 http.sys |
| Keep-Alive | ✅ 原生支持 | ✅ 支持 |
| 性能 | 更高（零拷贝潜力） | 中等 |
| 跨平台 | ✅ 完全跨平台 | ⚠️ Windows 最佳 |
| 管理员权限 | ❌ 不需要 | ⚠️ 某些场景需要 |

## 📁 项目结构

```
MiniGin/
├── MiniGin/                                    # 核心框架
│   ├── Context.cs                              # 请求上下文
│   ├── Engine.cs                               # HTTP 引擎
│   ├── Http/                                   # 🔥 Socket HTTP 服务器
│   │   └── HttpServer.cs                       # TCP Socket 实现
│   ├── RouterGroup.cs                          # 路由分组
│   ├── Middleware.cs                           # 内置中间件
│   ├── Gin.cs                                  # 工厂方法
│   └── Mvc/                                    # 🎮 控制器模式
│       ├── ControllerBase.cs                   # 控制器基类
│       ├── Attributes.cs                       # 路由特性
│       └── ControllerExtensions.cs             # 控制器扫描
├── MiniGin.Extensions.DependencyInjection/     # 🔌 依赖注入
├── MiniGin.Extensions.Hosting/                 # ⏰ 后台服务
├── MiniGin.Extensions.Data/                    # 🗄️ 数据库扩展
├── Demo/                                       # 示例代码
│   ├── Controllers/                            # 控制器示例
│   ├── Services/                               # 服务定义
│   ├── Routes/                                 # 模块化路由
│   └── Models/                                 # 请求模型
├── LuckyDraw/                                  # 🎲 DDD 示例
│   ├── Domain/                                 # 领域层
│   ├── Repository/                             # 仓储层
│   └── Services/                               # 服务层
└── Program.cs                                  # 入口文件
```

## 🚀 快速开始

### 1. 最简示例

```csharp
using MiniGin;

var app = Gin.Default();

app.GET("/", async ctx => await ctx.String(200, "Hello World!"));
app.GET("/ping", async ctx => await ctx.JSON(new { message = "pong" }));

await app.Run("http://localhost:5000/");
```

### 2. 完整示例（含 DI + 数据库 + 后台服务）

```csharp
using MiniGin;
using MiniGin.Mvc;
using MiniGin.Extensions.DependencyInjection;
using MiniGin.Extensions.Hosting;
using MiniGin.Extensions.Data;

var app = Gin.Default();
app.UseSwagger("Mini Gin API", "v1");

// 配置数据库
app.AddMySQL("Server=localhost;Database=MyDb;User=root;Password=123456;");

// 配置依赖注入
app.ConfigureServices(services =>
{
    services.AddSingleton<IGreetingService, GreetingService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IUserRepository, UserRepository>();
});

// 配置后台服务
app.AddHostedService<HeartbeatService>();

// 构建服务
app.BuildServices();

// 全局中间件
app.Use(Middleware.CORS(), Middleware.RequestId());

// 路由定义
app.GET("/", async ctx => await ctx.String(200, "API Ready!"));

var api = app.Group("/api");
api.MapUserRoutes();      // 函数式路由
api.MapAdminRoutes();     // 模块化路由

// 控制器模式
app.MapController<UserController>();

// 启动
await app.RunWithHostedServicesAsync("http://localhost:5000/");
```

## 📖 API 参考

### 路由定义

```csharp
// 基本路由
app.GET("/users", async ctx => { ... });
app.POST("/users", async ctx => { ... });
app.PUT("/users/:id", async ctx => { ... });
app.DELETE("/users/:id", async ctx => { ... });
app.PATCH("/users/:id", async ctx => { ... });

// 通用方法
app.Handle("GET", "/custom", async ctx => { ... });

// 多处理器链（中间件 + 处理器）
app.GET("/protected", authMiddleware, async ctx => { ... });
```

### 路由分组

```csharp
var api = app.Group("/api");
api.Use(myMiddleware);

// 嵌套分组
var admin = api.Group("/admin");
admin.Use(Middleware.BasicAuth((u, p) => u == "admin" && p == "secret"));

admin.GET("/dashboard", async ctx => {
    await ctx.JSON(new { message = "Admin Dashboard" });
});
```

### Context API

```csharp
// 获取路径参数
var id = ctx.Param("id");

// 获取查询参数
var page = ctx.Query<int>("page") ?? 1;
var name = ctx.Query("name"); // string?

// 绑定 JSON 请求体
var user = await ctx.BindAsync<CreateUserRequest>();

// 获取原始请求体
var body = await ctx.GetRawDataAsync();

// 设置响应头
ctx.Header("X-Custom-Header", "value");

// JSON 响应
await ctx.JSON(new { message = "success" });

// 文本响应
await ctx.String(200, "Hello World");

// 状态码快捷方法
await ctx.OK(data);           // 200
await ctx.Created(data);      // 201
await ctx.NoContent();        // 204
await ctx.BadRequest(error);  // 400
await ctx.NotFound();         // 404
await ctx.InternalServerError(error); // 500

// 存取上下文数据
ctx.Set("user", currentUser);
var user = ctx.Get<User>("user");

// 中止后续处理器
ctx.Abort();
ctx.AbortWithStatus(403);
await ctx.AbortWithError(403, "Forbidden");
```

### 内置中间件

```csharp
// Logger - 请求日志
app.Use(Middleware.Logger());

// Recovery - 异常恢复
app.Use(Middleware.Recovery());

// CORS - 跨域支持
app.Use(Middleware.CORS());
app.Use(Middleware.CORS(new CorsConfig {
    AllowOrigins = new[] { "https://example.com" },
    AllowMethods = new[] { "GET", "POST" },
    AllowHeaders = new[] { "Authorization" }
}));

// BasicAuth - HTTP 基本认证
app.Use(Middleware.BasicAuth((username, password) => 
    username == "admin" && password == "secret"));

// ApiKey - API 密钥认证
app.Use(Middleware.ApiKey("X-Api-Key", key => key == "my-secret-key"));

// Static - 静态文件服务
app.Use(Middleware.Static("/static", "./wwwroot"));

// RequestId - 请求 ID
app.Use(Middleware.RequestId());

// Timeout - 请求超时
app.Use(Middleware.Timeout(TimeSpan.FromSeconds(30)));
```

### 自定义中间件

```csharp
// 函数式中间件
app.Use(async ctx => {
    Console.WriteLine($"Before: {ctx.Request.Url}");
    // 继续执行后续处理器（通过不调用 Abort）
});

// 实现 IMiddleware 接口
public class MyMiddleware : IMiddleware
{
    public HandlerFunc Handler => async ctx => {
        // 前置逻辑
        ctx.Set("start_time", DateTime.Now);
        
        // 后续处理器会自动执行
        // 如果需要中止，调用 ctx.Abort()
    };
}

app.Use(new MyMiddleware());
```

## 🎮 控制器模式

除了函数式路由，MiniGin 还支持类似 ASP.NET Core 的控制器写法：

### 定义控制器

```csharp
using MiniGin.Mvc;

[Route("/api/users")]
public class UserController : ControllerBase
{
    [HttpGet]
    public async Task GetAll()
    {
        var service = GetService<IUserService>();
        var users = await service!.GetUsersAsync();
        await Ok(new { success = true, data = users });
    }

    [HttpGet(":id")]
    public async Task GetById()
    {
        var id = int.Parse(Param("id") ?? "0");
        var service = GetService<IUserService>();
        var user = await service!.GetByIdAsync(id);
        
        if (user == null)
        {
            await NotFound(new { message = "用户不存在" });
            return;
        }
        await Ok(new { success = true, data = user });
    }

    [HttpPost]
    public async Task Create()
    {
        var request = await BindAsync<CreateUserDto>();
        if (request == null)
        {
            await BadRequest(new { message = "无效请求" });
            return;
        }
        // ... 创建逻辑
        await Created(new { id = 1, name = request.Name });
    }

    [HttpDelete(":id")]
    public async Task Delete()
    {
        var id = int.Parse(Param("id") ?? "0");
        // ... 删除逻辑
        await NoContent();
    }
}
```

### 注册控制器

```csharp
// 方式一：注册单个控制器
app.MapController<UserController>();

// 方式二：自动扫描注册所有控制器
app.MapControllers();
```

### 可用特性

| 特性 | 说明 |
|------|------|
| `[Route("/path")]` | 控制器路由前缀 |
| `[HttpGet("path")]` | GET 方法 |
| `[HttpPost("path")]` | POST 方法 |
| `[HttpPut("path")]` | PUT 方法 |
| `[HttpDelete("path")]` | DELETE 方法 |
| `[HttpPatch("path")]` | PATCH 方法 |

### ControllerBase 方法

```csharp
// 响应方法
await Ok(data);           // 200
await Created(data);      // 201
await NoContent();        // 204
await BadRequest(error);  // 400
await NotFound(error);    // 404
await Json(data, 200);    // 自定义状态码

// 参数获取
var id = Param("id");              // 路由参数
var page = Query<int>("page");     // 查询参数
var body = await BindAsync<T>();   // 请求体绑定

// 服务获取
var service = GetService<IMyService>();
```

## 🔌 依赖注入

MiniGin 内置轻量级 DI 容器，支持三种生命周期：

```csharp
app.ConfigureServices(services =>
{
    // 单例 - 全局唯一实例
    services.AddSingleton<IConfig, AppConfig>();
    services.AddSingleton(new AppConfig());  // 实例注册
    
    // 作用域 - 每个请求一个实例
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IRepository, Repository>();
    
    // 瞬态 - 每次获取新实例
    services.AddTransient<IValidator, Validator>();
});

// 必须在配置完成后调用
app.BuildServices();

// 在路由中使用
app.GET("/users", async ctx =>
{
    var service = ctx.GetService<IUserService>();
    // ...
});
```

## 🗄️ 数据库支持

MiniGin 支持多种数据库，基于 ADO.NET + Dapper：

```csharp
// MySQL
app.AddMySQL("Server=localhost;Database=MyDb;User=root;Password=123456;");

// SQLite
app.AddSQLite("Data Source=app.db");

// SQL Server
app.AddSqlServer("Server=localhost;Database=MyDb;Trusted_Connection=True;");

// PostgreSQL
app.AddPostgreSQL("Host=localhost;Database=MyDb;Username=postgres;Password=123456;");

// 内存数据库（测试用）
app.AddInMemoryDatabase("TestDb");
```

### 使用仓储模式

```csharp
public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        return await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }
}
```

## ⏰ 后台服务

支持托管服务和定时任务：

```csharp
// 注册后台服务
app.AddHostedService<HeartbeatService>();
app.AddHostedService<CleanupService>();

// 启动（包含后台服务）
await app.RunWithHostedServicesAsync("http://localhost:5000/");
```

### 定时任务示例

```csharp
public class HeartbeatService : TimedBackgroundService
{
    protected override TimeSpan Interval => TimeSpan.FromSeconds(30);
    protected override bool ExecuteImmediately => false;

    protected override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"[Heartbeat] {DateTime.Now:HH:mm:ss} - Server is alive");
        return Task.CompletedTask;
    }
}

public class CleanupService : TimedBackgroundService
{
    protected override TimeSpan Interval => TimeSpan.FromMinutes(5);
    protected override bool ExecuteImmediately => true;

    protected override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        // 清理过期数据、临时文件等
        return Task.CompletedTask;
    }
}
```

## 📚 Swagger

```csharp
// 启用 Swagger
app.UseSwagger("API Title", "v1");

// 访问地址
// Swagger UI: http://localhost:5000/swagger
// OpenAPI JSON: http://localhost:5000/swagger/v1/swagger.json
```

## 🎲 DDD 示例：抽奖系统

项目包含一个完整的 DDD 架构示例 - 抽奖系统：

```
LuckyDraw/
├── Domain/
│   ├── Entities/
│   │   ├── LuckyDrawActivity.cs    # 聚合根
│   │   └── Participant.cs          # 参与者实体
│   └── ValueObjects/
│       └── DrawResult.cs           # 值对象
├── Repository/
│   ├── IRepository.cs              # 仓储接口
│   └── LuckyDrawRepository.cs      # 仓储实现
├── Services/
│   ├── ILuckyDrawService.cs        # 服务接口
│   └── LuckyDrawService.cs         # 服务实现
└── init_mysql.sql                  # 数据库脚本
```

### API 接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/lucky-draw/activities` | 获取所有活动 |
| GET | `/api/lucky-draw/activities/:id` | 获取活动详情 |
| POST | `/api/lucky-draw/activities` | 创建活动 |
| POST | `/api/lucky-draw/activities/:id/start` | 开始活动 |
| POST | `/api/lucky-draw/activities/:id/join` | 参与抽奖 |
| POST | `/api/lucky-draw/activities/:id/draw` | 🎲 执行抽奖 |
| GET | `/api/lucky-draw/activities/:id/result` | 获取结果 |

## 🛠️ 模块化路由

将路由组织成独立模块：

```csharp
// Demo/Routes/UserRoutes.cs
public static class UserRoutes
{
    public static void MapUserRoutes(this RouterGroup api)
    {
        api.GET("/users", async ctx => { ... });
        api.POST("/users", async ctx => { ... });
        api.GET("/users/:id", async ctx => { ... });
    }
}

// Program.cs
var api = app.Group("/api");
api.MapUserRoutes();
api.MapAdminRoutes();
api.MapLuckyDrawRoutes();
```

## 🏃 运行

```powershell
dotnet run --project MiniGin.Demo.csproj
```

- API 地址：`http://localhost:5000/`
- Swagger UI：`http://localhost:5000/swagger`

## 📦 打包为 NuGet

```powershell
cd MiniGin
dotnet pack -c Release
```

生成的 `.nupkg` 文件位于 `MiniGin/bin/Release/`。

## 📋 完整示例

```csharp
using MiniGin;
using MiniGin.Mvc;
using MiniGin.Extensions.DependencyInjection;
using MiniGin.Extensions.Hosting;
using MiniGin.Extensions.Data;

var app = Gin.Default();
app.UseSwagger("Mini Gin API", "v1");

// ========== 配置数据库 ==========
app.AddMySQL("Server=localhost;Database=MyDb;User=root;Password=123456;");

// ========== 配置依赖注入 ==========
app.ConfigureServices(services =>
{
    services.AddSingleton<IGreetingService, GreetingService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<ILuckyDrawRepository, LuckyDrawRepository>();
    services.AddScoped<ILuckyDrawService, LuckyDrawService>();
});

// ========== 配置后台服务 ==========
app.AddHostedService<HeartbeatService>();

// 构建服务
app.BuildServices();

// ========== 全局中间件 ==========
app.Use(Middleware.CORS(), Middleware.RequestId());

// ========== 路由定义 ==========
app.GET("/", async ctx => await ctx.String(200, "Mini Gin is ready!"));
app.GET("/ping", async ctx => await ctx.JSON(new { message = "pong" }));

// API 分组 + 模块化路由
var api = app.Group("/api");
api.Use(ctx => { ctx.Header("X-Api-Version", "1.0"); return Task.CompletedTask; });

api.MapUserRoutes();
api.MapLuckyDrawRoutes();

// 控制器模式
app.MapController<LuckyDrawController>();

// ========== 启动 ==========
await app.RunWithHostedServicesAsync("http://localhost:5000/");
```

## 📜 许可证

MIT
