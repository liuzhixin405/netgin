using NetWeb.Extensions.DependencyInjection;

namespace NetWeb.Extensions.Data;

/// <summary>
/// Engine 扩展方法 - 添加数据库支持
/// </summary>
public static class EngineDataExtensions
{
    /// <summary>
    /// 添加数据库支持
    /// </summary>
    public static Engine AddDatabase(this Engine engine, DatabaseOptions options)
    {
        engine.ConfigureServices(services =>
        {
            // 注册数据库配置
            services.AddSingleton(options);

            // 注册连接工厂
            services.AddSingleton<IDbConnectionFactory>(sp => new DbConnectionFactory(options));

            // 注册 SQL 方言
            services.AddSingleton<ISqlDialect>(sp => SqlDialectFactory.GetDialect(options.DatabaseType));

            // 注册工作单元（Scoped）
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        });

        return engine;
    }

    /// <summary>
    /// 添加 SQLite 数据库
    /// </summary>
    public static Engine AddSQLite(this Engine engine, string connectionString = "Data Source=app.db")
        => engine.AddDatabase(DatabaseOptions.UseSQLite(connectionString));

    /// <summary>
    /// 添加 SQL Server 数据库
    /// </summary>
    public static Engine AddSqlServer(this Engine engine, string connectionString)
        => engine.AddDatabase(DatabaseOptions.UseSqlServer(connectionString));

    /// <summary>
    /// 添加 MySQL 数据库
    /// </summary>
    public static Engine AddMySQL(this Engine engine, string connectionString)
        => engine.AddDatabase(DatabaseOptions.UseMySQL(connectionString));

    /// <summary>
    /// 添加 PostgreSQL 数据库
    /// </summary>
    public static Engine AddPostgreSQL(this Engine engine, string connectionString)
        => engine.AddDatabase(DatabaseOptions.UsePostgreSQL(connectionString));

    /// <summary>
    /// 添加内存数据库（用于测试）
    /// </summary>
    public static Engine AddInMemoryDatabase(this Engine engine, string databaseName = "InMemoryDb")
        => engine.AddDatabase(DatabaseOptions.UseInMemory(databaseName));

    /// <summary>
    /// 添加仓储
    /// </summary>
    public static Engine AddRepository<TInterface, TImplementation>(this Engine engine)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        engine.ConfigureServices(services =>
        {
            services.AddScoped<TInterface, TImplementation>();
        });
        return engine;
    }
}
