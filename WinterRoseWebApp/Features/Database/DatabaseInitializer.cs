namespace WinterRoseWebApp.Features.Database;

using Npgsql;

public static class DatabaseInitializer
{
    public static void EnsureDatabaseCreated(string connectionString)
    {
        // Parse the connection string to extract database and user info
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        var username = builder.Username;
        var password = builder.Password;
        var host = builder.Host;
        var port = builder.Port;

        // Connect to the default 'postgres' database to create user/database
        var masterConnectionString = $"Host={host};Port={port};Database=postgres;Username=postgres;Password=your_postgres_password";

        using (var conn = new NpgsqlConnection(masterConnectionString))
        {
            conn.Open();

            // 1. Check if the user exists
            var userExists = false;
            using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_roles WHERE rolname = @username", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                userExists = cmd.ExecuteScalar() != null;
            }

            // 2. Create the user if it doesn't exist
            if (!userExists)
            {
                using (var cmd = new NpgsqlCommand($"CREATE ROLE {username} WITH LOGIN PASSWORD @password CREATEDB", conn))
                {
                    cmd.Parameters.AddWithValue("password", password);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine($"✅ User '{username}' created.");
            }

            // 3. Check if the database exists
            var dbExists = false;
            using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @databaseName", conn))
            {
                cmd.Parameters.AddWithValue("databaseName", databaseName);
                dbExists = cmd.ExecuteScalar() != null;
            }

            // 4. Create the database if it doesn't exist
            if (!dbExists)
            {
                using (var cmd = new NpgsqlCommand($"CREATE DATABASE {databaseName} OWNER {username}", conn))
                {
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine($"✅ Database '{databaseName}' created.");
            }
        }
    }
}