using NetWeb;
using NetWeb.Mvc;
using NetWeb.Extensions.DependencyInjection;
using NetWeb.Extensions.Hosting;
using NetWeb.Extensions.Data;
using NetWeb.Demo.Services;
using NetWeb.Demo.HostedServices;
using NetWeb.Demo.Routes;
using NetWeb.Demo.Controllers;
using LuckyDraw.Repository;
using LuckyDraw.Services;
using Microsoft.Extensions.Configuration;

// 创建引擎（类似 gin.Default()）
var app = Gin.Default();

// ========== 0. 读取配置（INI） ==========
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddIniFile("appsettings.ini", optional: true, reloadOnChange: true)
    .Build();

var serverUrl = configuration["Server:Url"] ?? "http://localhost:5000";

// 启用 Swagger
app.UseSwagger("Mini Gin API", "v1");

// ========== 1. 配置数据库（必须在依赖它的服务之前） ==========
app.AddDatabaseFromConfiguration(configuration);
// ========== 2. 配置依赖注入 ==========
app.ConfigureServices(services =>
{
    // 注册自定义服务
    services.AddSingleton<IGreetingService, GreetingService>();
    services.AddScoped<IUserService, UserService>();
    
    // 🎲 注册抽奖系统服务（依赖 IDbConnectionFactory）
    services.AddScoped<ILuckyDrawRepository, LuckyDrawRepository>();
    services.AddScoped<IParticipantRepository, ParticipantRepository>();
    services.AddScoped<ILuckyDrawService, LuckyDrawService>();
});

// ========== 3. 配置后台服务 ==========
app.AddHostedService<HeartbeatService>();
app.AddHostedService<CleanupService>();

// 构建服务（必须在所有配置之后调用）
app.BuildServices();

// ========== 4. 全局中间件 ==========
app.Use(
    Middleware.CORS(),
    Middleware.RequestId()
);

// ========== 5. 路由定义 ==========
// 根路由
app.GET("/", async ctx => await ctx.String(200, "Mini Gin is ready!"));
app.GET("/ping", async ctx => await ctx.JSON(new { message = "pong" }));

// 测试 DI 的路由
app.GET("/greeting", async ctx =>
{
    var greetingService = ctx.GetService<IGreetingService>();
    var message = greetingService?.GetGreeting("World") ?? "Service not found";
    await ctx.JSON(new { message });
});

// API 分组
var api = app.Group("/api");
api.Use(ctx =>
{
    ctx.Header("X-Api-Version", "1.0");
    return Task.CompletedTask;
});

// 注册模块化路由
api.MapUserRoutes();      // 用户路由
api.MapAdminRoutes();     // 管理员路由
api.MapLuckyDrawRoutes(); // 🎲 抽奖系统路由（函数式）

// ========== 🎮 控制器模式 ==========
// 方式一：注册单个控制器
app.MapController<LuckyDrawController>();

// 方式二：自动扫描并注册所有控制器
// app.MapControllers();

// ========== 6. 启动服务器（包含后台服务） ==========
await app.RunWithHostedServicesAsync(serverUrl);