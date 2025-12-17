namespace NetWeb.Extensions.Data;

/// <summary>
/// 数据库类型
/// </summary>
public enum DatabaseType
{
    /// <summary>SQLite</summary>
    SQLite,
    
    /// <summary>SQL Server</summary>
    SqlServer,
    
    /// <summary>MySQL / MariaDB</summary>
    MySQL,
    
    /// <summary>PostgreSQL</summary>
    PostgreSQL,

    /// <summary>内存数据库（用于测试）</summary>
    InMemory
}
