using OOs.CommandLine;

namespace Mqtt.Server.Migrate;

[Option<string>("DbProvider", "provider", ShortAlias = 'p',
    Hint = "provider",
    Description = "Database provider to use. Allowed values are Sqlite, SQLite, PostgreSQL, Npgsql, MSSQL, SqlServer, CosmosDB.")]
[Option<string>("ConnectionStrings:AppDbContextConnection", "connection", ShortAlias = 'c',
    Hint = "connection_string", Description = "The connection string to the database.")]
[Option<Microsoft.Extensions.Logging.LogLevel>("Logging:LogLevel:Default", "verbosity", ShortAlias = 'v',
    Hint = "level", Description = "Set verbosity (default log level).")]
[ArgumentParserGenerationOptions(
    GenerateSynopsis = true,
    AddStandardOptions = true,
    UnknownOptionBehavior = UnknownOptionBehavior.Preserve)]
internal partial struct Arguments { }