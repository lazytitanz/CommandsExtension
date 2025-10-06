using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace CommandsExtension
{
    /// <summary>
    /// Manages SQLite database initialization and connection for storing custom commands and scheduled tasks.
    /// Creates the database in the PluginData folder if it doesn't exist.
    /// </summary>
    internal class DatabaseHelper
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        /// <summary>
        /// Initializes the database helper and creates necessary tables if they don't exist.
        /// </summary>
        public DatabaseHelper()
        {
            // Get the bot's working directory and create PluginData folder if needed
            string workingDirectory = Directory.GetCurrentDirectory();
            string pluginDataFolder = Path.Combine(workingDirectory, "PluginData");

            if (!Directory.Exists(pluginDataFolder))
            {
                Directory.CreateDirectory(pluginDataFolder);
            }

            // Set up database path and connection string
            _databasePath = Path.Combine(pluginDataFolder, "commandextensionsplugin.db");
            _connectionString = $"Data Source={_databasePath}";

            // Initialize database tables
            InitializeDatabase();
        }

        /// <summary>
        /// Creates the CustomCommands and ScheduledTasks tables if they don't exist.
        /// </summary>
        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Create CustomCommands table for !commands
                var createCustomCommandsTable = connection.CreateCommand();
                createCustomCommandsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CustomCommands (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CommandName TEXT NOT NULL UNIQUE,
                        Response TEXT NOT NULL,
                        RequiredRole TEXT NOT NULL DEFAULT 'Everyone',
                        UserCooldownSeconds INTEGER NOT NULL DEFAULT 5,
                        GlobalCooldownSeconds INTEGER NOT NULL DEFAULT 0,
                        IsEnabled INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    )";
                createCustomCommandsTable.ExecuteNonQuery();

                // Create ScheduledTasks table for timed/repeating commands
                var createScheduledTasksTable = connection.CreateCommand();
                createScheduledTasksTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ScheduledTasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TaskName TEXT NOT NULL UNIQUE,
                        Message TEXT NOT NULL,
                        IntervalMinutes INTEGER NOT NULL,
                        IsEnabled INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    )";
                createScheduledTasksTable.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets the SQLite connection string.
        /// </summary>
        public string GetConnectionString() => _connectionString;

        /// <summary>
        /// Gets the full path to the database file.
        /// </summary>
        public string GetDatabasePath() => _databasePath;
    }
}
