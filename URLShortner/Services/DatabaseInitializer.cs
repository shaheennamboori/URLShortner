using Npgsql;
using Dapper;
namespace URLShortner.Services;

public class DatabaseInitializer(NpgsqlDataSource dataSource, IConfiguration configuration, ILogger<DatabaseInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CreateDatabaseIfNotExists(stoppingToken);
            await InitializeSchema(stoppingToken);

            logger.LogInformation("Database Initialization completed successfully");
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error Initializing database");
            throw;
        }
    }

    private async Task InitializeSchema(CancellationToken stoppingToken)
    {
        const string createTableSql = """
            Create table If Not Exists shortened_urls (
            id SERIAL PRIMARY KEY,
            short_code VARCHAR(10) UNIQUE NOT NULL,
            original_url Text Not Null,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX if not exists idx_short_code on shortened_urls(short_code);

            create table if not exists url_visits (
            id serial primary key,
            short_code varchar(10) not null,
            visited_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            user_agent TEXT,
            referer TEXT,
            Foreign key (short_code) references shortened_urls(short_code)
            );

            Create index if not exists idx_visits_short_code On url_visits(short_code);
            """;

        await using var command = dataSource.CreateCommand(createTableSql);
        command.ExecuteNonQuery();
    }

    private async Task CreateDatabaseIfNotExists(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetConnectionString("url-shortner");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        string? databaseName = builder.Database;
        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ToString());
        await connection.OpenAsync(stoppingToken);

        bool databaseExists = await connection.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = @databaseName)", new { databaseName});

        if (!databaseExists) { 
            logger.LogInformation("Creating database: {DatabaseName}", databaseName);
            string sql = $"CREATE DATABASE \"{databaseName}\"";
            await connection.ExecuteAsync(sql);
        }
    }
}
