using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TwitchChatBot;

namespace CommandsExtension
{
    /// <summary>
    /// Manages scheduled tasks by registering them with the bot's built-in scheduler.
    /// Loads tasks from database and uses SDK.ScheduleJob() for execution.
    /// </summary>
    internal class TaskScheduler
    {
        private readonly DatabaseHelper _db;
        private readonly BotSDK _sdk;
        private readonly string _pluginName;
        private readonly HashSet<string> _registeredTasks = new HashSet<string>();

        /// <summary>
        /// Creates a new task scheduler instance.
        /// </summary>
        /// <param name="db">Database helper for accessing task data.</param>
        /// <param name="sdk">Bot SDK for scheduling jobs.</param>
        /// <param name="pluginName">Name of the plugin for logging.</param>
        public TaskScheduler(DatabaseHelper db, BotSDK sdk, string pluginName)
        {
            _db = db;
            _sdk = sdk;
            _pluginName = pluginName;
        }

        /// <summary>
        /// Loads all enabled scheduled tasks from the database and registers them with the bot scheduler.
        /// </summary>
        public void Start()
        {
            try
            {
                using (var connection = new SqliteConnection(_db.GetConnectionString()))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT TaskName, Message, IntervalMinutes FROM ScheduledTasks WHERE IsEnabled = 1";

                    using (var reader = command.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            string taskName = reader.GetString(0);
                            string message = reader.GetString(1);
                            int intervalMinutes = reader.GetInt32(2);

                            var scheduledCommand = new ScheduledTaskCommand(message);
                            _sdk.ScheduleJob(taskName, intervalMinutes, scheduledCommand);
                            _registeredTasks.Add(taskName);
                            count++;
                        }
                        if (count > 0)
                        {
                            _sdk.LogInfo(_pluginName, "Loaded " + count + " scheduled task(s)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Escape square brackets for Spectre.Console markup
                string safeMessage = ex.Message.Replace("[", "[[").Replace("]", "]]");
                _sdk.LogError(_pluginName, "Failed to load scheduled tasks: " + safeMessage);
            }
        }

        /// <summary>
        /// Stops all running scheduled tasks by removing them from the bot scheduler.
        /// </summary>
        public void Stop()
        {
            int count = _registeredTasks.Count;
            foreach (var taskName in _registeredTasks)
            {
                try
                {
                    _sdk.RemoveScheduledJob(taskName);
                }
                catch
                {
                    // Silently ignore errors when removing tasks that may not exist
                    // This can happen during reload operations
                }
            }
            _registeredTasks.Clear();
        }

        /// <summary>
        /// Reloads all scheduled tasks from the database.
        /// Stops existing tasks and starts new ones based on current database state.
        /// </summary>
        public void ReloadTasks()
        {
            try
            {
                Stop();
                Start();
                _sdk.LogInfo(_pluginName, "Scheduled tasks reloaded");
            }
            catch (Exception ex)
            {
                // Escape square brackets for Spectre.Console markup
                string safeMessage = ex.Message.Replace("[", "[[").Replace("]", "]]");
                _sdk.LogError(_pluginName, "Failed to reload scheduled tasks: " + safeMessage);
            }
        }
    }
}
