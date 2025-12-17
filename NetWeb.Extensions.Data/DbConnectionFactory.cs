using System.Data;

namespace NetWeb.Extensions.Data;

/// <summary>
/// 数据库连接工厂接口
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// 创建数据库连接
    /// </summary>
    IDbConnection CreateConnection();

    /// <summary>
    /// 获取数据库类型
    /// </summary>
    DatabaseType DatabaseType { get; }

    /// <summary>
    /// 获取数据库选项
    /// </summary>
    DatabaseOptions Options { get; }
}

/// <summary>
/// 数据库连接工厂
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseOptions _options;
    private readonly Func<string, IDbConnection> _connectionFactory;

    public DatabaseType DatabaseType => _options.DatabaseType;
    public DatabaseOptions Options => _options;

    public DbConnectionFactory(DatabaseOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionFactory = CreateConnectionFactory(options.DatabaseType);
    }

    public IDbConnection CreateConnection()
    {
        var connection = _connectionFactory(_options.ConnectionString);
        return connection;
    }

    private static Func<string, IDbConnection> CreateConnectionFactory(DatabaseType dbType)
    {
        // 返回一个工厂函数，实际的连接类型需要用户通过扩展方法注册
        return dbType switch
        {
            DatabaseType.SQLite => connectionString => CreateSQLiteConnection(connectionString),
            DatabaseType.SqlServer => connectionString => CreateSqlServerConnection(connectionString),
            DatabaseType.MySQL => connectionString => CreateMySqlConnection(connectionString),
            DatabaseType.PostgreSQL => connectionString => CreatePostgreSqlConnection(connectionString),
            DatabaseType.InMemory => connectionString => CreateInMemoryConnection(connectionString),
            _ => throw new NotSupportedException($"Database type '{dbType}' is not supported.")
        };
    }

    // 这些方法使用反射来创建连接，避免直接依赖特定的 ADO.NET 提供程序
    private static IDbConnection CreateSQLiteConnection(string connectionString)
    {
        return CreateConnectionByTypeName(
            "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite",
            connectionString,
            "SQLite");
    }

    private static IDbConnection CreateSqlServerConnection(string connectionString)
    {
        return CreateConnectionByTypeName(
            "Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient",
            connectionString,
            "SQL Server");
    }

    private static IDbConnection CreateMySqlConnection(string connectionString)
    {
        return CreateConnectionByTypeName(
            "MySqlConnector.MySqlConnection, MySqlConnector",
            connectionString,
            "MySQL") ?? CreateConnectionByTypeName(
            "MySql.Data.MySqlClient.MySqlConnection, MySql.Data",
            connectionString,
            "MySQL");
    }

    private static IDbConnection CreatePostgreSqlConnection(string connectionString)
    {
        return CreateConnectionByTypeName(
            "Npgsql.NpgsqlConnection, Npgsql",
            connectionString,
            "PostgreSQL");
    }

    private static IDbConnection CreateInMemoryConnection(string connectionString)
    {
        // SQLite 内存模式
        return CreateConnectionByTypeName(
            "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite",
            $"Data Source={connectionString};Mode=Memory;Cache=Shared",
            "SQLite InMemory");
    }

    private static IDbConnection CreateConnectionByTypeName(string typeName, string connectionString, string dbName)
    {
        var type = Type.GetType(typeName);
        if (type == null)
        {
            throw new InvalidOperationException(
                $"无法找到 {dbName} 连接类型。请确保已安装对应的 NuGet 包。\n" +
                $"提示：{GetInstallHint(dbName)}");
        }

        var connection = Activator.CreateInstance(type, connectionString) as IDbConnection;
        if (connection == null)
        {
            throw new InvalidOperationException($"无法创建 {dbName} 连接实例。");
        }

        return connection;
    }

    private static string GetInstallHint(string dbName) => dbName switch
    {
        "SQLite" or "SQLite InMemory" => "dotnet add package Microsoft.Data.Sqlite",
        "SQL Server" => "dotnet add package Microsoft.Data.SqlClient",
        "MySQL" => "dotnet add package MySqlConnector 或 dotnet add package MySql.Data",
        "PostgreSQL" => "dotnet add package Npgsql",
        _ => "请安装对应的 ADO.NET 提供程序"
    };
}
