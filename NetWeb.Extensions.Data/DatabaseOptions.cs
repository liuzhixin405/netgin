namespace NetWeb.Extensions.Data;

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLite;

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=app.db";

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// 命令超时时间（秒）
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// 连接池最小连接数
    /// </summary>
    public int MinPoolSize { get; set; } = 1;

    /// <summary>
    /// 连接池最大连接数
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// 创建 SQLite 配置
    /// </summary>
    public static DatabaseOptions UseSQLite(string connectionString = "Data Source=app.db")
        => new() { DatabaseType = DatabaseType.SQLite, ConnectionString = connectionString };

    /// <summary>
    /// 创建 SQL Server 配置
    /// </summary>
    public static DatabaseOptions UseSqlServer(string connectionString)
        => new() { DatabaseType = DatabaseType.SqlServer, ConnectionString = connectionString };

    /// <summary>
    /// 创建 MySQL 配置
    /// </summary>
    public static DatabaseOptions UseMySQL(string connectionString)
        => new() { DatabaseType = DatabaseType.MySQL, ConnectionString = connectionString };

    /// <summary>
    /// 创建 PostgreSQL 配置
    /// </summary>
    public static DatabaseOptions UsePostgreSQL(string connectionString)
        => new() { DatabaseType = DatabaseType.PostgreSQL, ConnectionString = connectionString };

    /// <summary>
    /// 创建内存数据库配置
    /// </summary>
    public static DatabaseOptions UseInMemory(string databaseName = "InMemoryDb")
        => new() { DatabaseType = DatabaseType.InMemory, ConnectionString = databaseName };
}
