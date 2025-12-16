namespace MiniGin.Extensions.Data;

/// <summary>
/// SQL 方言接口 - 处理不同数据库的 SQL 差异
/// </summary>
public interface ISqlDialect
{
    /// <summary>
    /// 获取参数前缀（如 @ 或 :）
    /// </summary>
    string ParameterPrefix { get; }

    /// <summary>
    /// 获取标识符引用符（如 [] 或 ``）
    /// </summary>
    (string Open, string Close) IdentifierQuote { get; }

    /// <summary>
    /// 获取分页 SQL
    /// </summary>
    string GetPagingSql(string sql, int offset, int limit);

    /// <summary>
    /// 获取插入后返回 ID 的 SQL
    /// </summary>
    string GetInsertReturnIdSql(string insertSql);

    /// <summary>
    /// 获取判断表是否存在的 SQL
    /// </summary>
    string GetTableExistsSql(string tableName);

    /// <summary>
    /// 引用标识符（表名、列名）
    /// </summary>
    string QuoteIdentifier(string identifier);
}

/// <summary>
/// SQLite 方言
/// </summary>
public class SQLiteDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public (string Open, string Close) IdentifierQuote => ("\"", "\"");

    public string GetPagingSql(string sql, int offset, int limit)
        => $"{sql} LIMIT {limit} OFFSET {offset}";

    public string GetInsertReturnIdSql(string insertSql)
        => $"{insertSql}; SELECT last_insert_rowid();";

    public string GetTableExistsSql(string tableName)
        => $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";

    public string QuoteIdentifier(string identifier)
        => $"\"{identifier}\"";
}

/// <summary>
/// SQL Server 方言
/// </summary>
public class SqlServerDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public (string Open, string Close) IdentifierQuote => ("[", "]");

    public string GetPagingSql(string sql, int offset, int limit)
        => $"{sql} OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";

    public string GetInsertReturnIdSql(string insertSql)
        => $"{insertSql}; SELECT SCOPE_IDENTITY();";

    public string GetTableExistsSql(string tableName)
        => $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";

    public string QuoteIdentifier(string identifier)
        => $"[{identifier}]";
}

/// <summary>
/// MySQL 方言
/// </summary>
public class MySqlDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public (string Open, string Close) IdentifierQuote => ("`", "`");

    public string GetPagingSql(string sql, int offset, int limit)
        => $"{sql} LIMIT {limit} OFFSET {offset}";

    public string GetInsertReturnIdSql(string insertSql)
        => $"{insertSql}; SELECT LAST_INSERT_ID();";

    public string GetTableExistsSql(string tableName)
        => $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";

    public string QuoteIdentifier(string identifier)
        => $"`{identifier}`";
}

/// <summary>
/// PostgreSQL 方言
/// </summary>
public class PostgreSqlDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public (string Open, string Close) IdentifierQuote => ("\"", "\"");

    public string GetPagingSql(string sql, int offset, int limit)
        => $"{sql} LIMIT {limit} OFFSET {offset}";

    public string GetInsertReturnIdSql(string insertSql)
        => $"{insertSql} RETURNING id";

    public string GetTableExistsSql(string tableName)
        => $"SELECT tablename FROM pg_tables WHERE tablename = '{tableName}'";

    public string QuoteIdentifier(string identifier)
        => $"\"{identifier}\"";
}

/// <summary>
/// SQL 方言工厂
/// </summary>
public static class SqlDialectFactory
{
    private static readonly Dictionary<DatabaseType, ISqlDialect> _dialects = new()
    {
        [DatabaseType.SQLite] = new SQLiteDialect(),
        [DatabaseType.SqlServer] = new SqlServerDialect(),
        [DatabaseType.MySQL] = new MySqlDialect(),
        [DatabaseType.PostgreSQL] = new PostgreSqlDialect(),
        [DatabaseType.InMemory] = new SQLiteDialect() // 内存模式使用 SQLite
    };

    /// <summary>
    /// 获取 SQL 方言
    /// </summary>
    public static ISqlDialect GetDialect(DatabaseType dbType)
    {
        if (_dialects.TryGetValue(dbType, out var dialect))
            return dialect;

        throw new NotSupportedException($"不支持的数据库类型：{dbType}");
    }

    /// <summary>
    /// 注册自定义方言
    /// </summary>
    public static void RegisterDialect(DatabaseType dbType, ISqlDialect dialect)
    {
        _dialects[dbType] = dialect;
    }
}
