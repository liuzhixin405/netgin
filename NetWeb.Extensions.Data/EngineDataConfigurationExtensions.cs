using Microsoft.Extensions.Configuration;

namespace NetWeb.Extensions.Data;

/// <summary>
/// 从配置读取数据库设置
/// </summary>
public static class EngineDataConfigurationExtensions
{
    /// <summary>
    /// 从配置中添加数据库支持。
    /// 
    /// 期望配置形如：
    /// [Database]
    /// Type=MySQL|PostgreSQL|SqlServer|SQLite|InMemory
    /// ConnectionString=...
    /// EnableLogging=true|false (可选)
    /// CommandTimeout=30 (可选)
    /// MinPoolSize=1 (可选)
    /// MaxPoolSize=100 (可选)
    /// </summary>
    public static Engine AddDatabaseFromConfiguration(this Engine engine, IConfiguration configuration, string sectionName = "Database")
    {
        if (engine == null) throw new ArgumentNullException(nameof(engine));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection(sectionName);

        var typeRaw = section["Type"] ?? section["DatabaseType"];
        if (string.IsNullOrWhiteSpace(typeRaw))
        {
            throw new InvalidOperationException($"缺少数据库类型配置：{sectionName}:Type。可选值：SQLite, SqlServer, MySQL, PostgreSQL, InMemory。");
        }

        if (!TryParseDatabaseType(typeRaw, out var dbType))
        {
            throw new InvalidOperationException($"不支持的数据库类型配置：{sectionName}:Type={typeRaw}。可选值：SQLite, SqlServer, MySQL, PostgreSQL, InMemory。");
        }

        var connectionString = section["ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"缺少连接字符串配置：{sectionName}:ConnectionString。");
        }

        var options = new DatabaseOptions
        {
            DatabaseType = dbType,
            ConnectionString = connectionString
        };

        if (TryGetBool(section, "EnableLogging", out var enableLogging))
            options.EnableLogging = enableLogging;

        if (TryGetInt(section, "CommandTimeout", out var commandTimeout))
            options.CommandTimeout = commandTimeout;

        if (TryGetInt(section, "MinPoolSize", out var minPoolSize))
            options.MinPoolSize = minPoolSize;

        if (TryGetInt(section, "MaxPoolSize", out var maxPoolSize))
            options.MaxPoolSize = maxPoolSize;

        return engine.AddDatabase(options);
    }

    private static bool TryParseDatabaseType(string value, out DatabaseType databaseType)
    {
        databaseType = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        return normalized.Equals("sqlite", StringComparison.OrdinalIgnoreCase) ? Set(DatabaseType.SQLite, out databaseType)
            : normalized.Equals("sqlserver", StringComparison.OrdinalIgnoreCase) || normalized.Equals("mssql", StringComparison.OrdinalIgnoreCase) ? Set(DatabaseType.SqlServer, out databaseType)
            : normalized.Equals("mysql", StringComparison.OrdinalIgnoreCase) || normalized.Equals("mariadb", StringComparison.OrdinalIgnoreCase) ? Set(DatabaseType.MySQL, out databaseType)
            : normalized.Equals("postgresql", StringComparison.OrdinalIgnoreCase) || normalized.Equals("postgres", StringComparison.OrdinalIgnoreCase) ? Set(DatabaseType.PostgreSQL, out databaseType)
            : normalized.Equals("inmemory", StringComparison.OrdinalIgnoreCase) || normalized.Equals("memory", StringComparison.OrdinalIgnoreCase) ? Set(DatabaseType.InMemory, out databaseType)
            : false;

        static bool Set(DatabaseType t, out DatabaseType dt)
        {
            dt = t;
            return true;
        }
    }

    private static bool TryGetBool(IConfiguration section, string key, out bool value)
    {
        value = default;
        var raw = section[key];
        return raw != null && bool.TryParse(raw, out value);
    }

    private static bool TryGetInt(IConfiguration section, string key, out int value)
    {
        value = default;
        var raw = section[key];
        return raw != null && int.TryParse(raw, out value);
    }
}
