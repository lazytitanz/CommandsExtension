using System;
using System.IO;
using Microsoft.Data.Sqlite;
using TwitchChatBot;

namespace CommandsExtension
{
    /// <summary>
    /// TwitchChatBot plugin that provides a web UI for managing custom chat commands.
    /// Allows creating, editing, and deleting commands dynamically without restarting the bot.
    /// </summary>
    public class CommandsExtensionPlugin : IBotPlugin
    {
        public string Name => "CommandsExtension";
        public string Version => "1.0.0";
        public string Author => "SilentNades";

        private BotSDK sdk;
        private DatabaseHelper db;
        private WebServer webServer;
        private System.Collections.Generic.HashSet<string> registeredCommands = new System.Collections.Generic.HashSet<string>();

        public void Initialize(BotSDK sdk)
        {
            this.sdk = sdk;

            try
            {
                // Initialize database
                db = new DatabaseHelper();

                // Load custom commands from database and register them
                LoadAndRegisterCommands();

                // Load web server port from appsettings.json
                int webServerPort = LoadWebServerPort();

                // Start web server
                webServer = new WebServer(webServerPort, db, this);
                webServer.Start();

                sdk.LogInfo(Name, "CommandsExtension plugin initialized successfully!");
                sdk.LogInfo(Name, $"Web UI: http://localhost:{webServerPort}");
            }
            catch (Exception ex)
            {
                sdk.LogError(Name, $"Failed to initialize: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads all enabled custom commands from the database and registers them with the bot.
        /// </summary>
        private void LoadAndRegisterCommands()
        {
            try
            {
                using (var connection = new SqliteConnection(db.GetConnectionString()))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT CommandName, Response, RequiredRole, UserCooldownSeconds, GlobalCooldownSeconds FROM CustomCommands WHERE IsEnabled = 1";

                    using (var reader = command.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            string commandName = reader.GetString(0);
                            string response = reader.GetString(1);
                            string requiredRole = reader.GetString(2);
                            int userCooldown = reader.GetInt32(3);
                            int globalCooldown = reader.GetInt32(4);

                            var dynamicCommand = new DynamicCommand(response, requiredRole, userCooldown, globalCooldown);
                            sdk.RegisterCommand(commandName, dynamicCommand);
                            registeredCommands.Add(commandName);
                            count++;
                        }
                        sdk.LogInfo(Name, $"Loaded {count} custom command(s) from database");
                    }
                }
            }
            catch (Exception ex)
            {
                sdk.LogError(Name, $"Failed to load commands: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the web server port from appsettings.json. Defaults to 5000 if not found.
        /// </summary>
        /// <returns>The port number for the web UI server.</returns>
        private int LoadWebServerPort()
        {
            try
            {
                string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                if (File.Exists(appSettingsPath))
                {
                    string jsonContent = File.ReadAllText(appSettingsPath);
                    var doc = System.Text.Json.JsonDocument.Parse(jsonContent);

                    if (doc.RootElement.TryGetProperty("Settings", out var settings))
                    {
                        if (settings.TryGetProperty("WebServerPort", out var port))
                        {
                            return port.GetInt32();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sdk.LogError(Name, $"Failed to load web server port from appsettings.json: {ex.Message}");
            }

            // Default to port 5000 if not specified
            return 5000;
        }

        /// <summary>
        /// Unregisters all current commands and reloads them from the database.
        /// Called automatically when commands are added or deleted via the web UI.
        /// </summary>
        public void ReloadCommands()
        {
            try
            {
                // Unregister all previously registered commands
                foreach (var commandName in registeredCommands)
                {
                    sdk.UnregisterCommand(commandName);
                }
                registeredCommands.Clear();

                // Reload commands from database
                LoadAndRegisterCommands();
                sdk.LogInfo(Name, "Commands reloaded successfully");
            }
            catch (Exception ex)
            {
                sdk.LogError(Name, $"Failed to reload commands: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            sdk.LogInfo(Name, "CommandsExtension plugin shutting down...");

            try
            {
                webServer?.Stop();
            }
            catch (Exception ex)
            {
                sdk.LogError(Name, $"Error during shutdown: {ex.Message}");
            }
        }
    }
}
