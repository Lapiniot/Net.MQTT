using Microsoft.Extensions.Logging;
using OOs.CommandLine;

namespace Mqtt.Server.Migrate;

[Option<string>("DbProvider", "provider", ShortAlias = 'p',
    Hint = "PROVIDER",
    Description = "Database provider to use. Allowed values are Sqlite, SQLite, PostgreSQL, Npgsql, MSSQL, SqlServer, CosmosDB.")]
[Option<string>("ConnectionStrings:AppDbContextConnection", "connection", ShortAlias = 'c',
    Hint = "CONNECTION_STRING",
    Description = "The connection string to the database.")]
[Option<LogLevel>("Logging:LogLevel:Default", "verbosity", ShortAlias = 'v',
    Hint = "LEVEL",
    Description = "Set verbosity (default log level). Allowed values are Trace, Debug, Information, Warning, Error, Critical, None.")]
[ArgumentParserGenerationOptions(
    GenerateSynopsis = true,
    AddStandardOptions = true,
    UnknownOptionBehavior = UnknownOptionBehavior.Preserve
)]
internal partial struct Arguments { }