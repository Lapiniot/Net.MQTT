namespace Mqtt.Server.Identity.Stores;

public enum DbProvider
{
    SQLite,
    PostgreSQL,
    Npgsql = PostgreSQL,
    MSSQL,
    SqlServer = MSSQL,
    CosmosDB
}