using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandsExtension
{
    /// <summary>
    /// HTTP server that provides a REST API and web UI for managing custom commands.
    /// Listens on localhost and automatically reloads commands when changes are made.
    /// </summary>
    internal class WebServer
    {
        private HttpListener _listener;
        private readonly int _port;
        private readonly DatabaseHelper _db;
        private readonly CommandsExtensionPlugin _plugin;
        private TaskScheduler _taskScheduler;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Creates a new web server instance.
        /// </summary>
        /// <param name="port">The port to listen on (e.g., 5000).</param>
        /// <param name="db">Database helper for accessing command data.</param>
        /// <param name="plugin">Plugin instance for triggering command reloads.</param>
        public WebServer(int port, DatabaseHelper db, CommandsExtensionPlugin plugin)
        {
            _port = port;
            _db = db;
            _plugin = plugin;
        }

        /// <summary>
        /// Sets the task scheduler instance for reload operations.
        /// Must be called after WebServer construction and before tasks are modified.
        /// </summary>
        /// <param name="taskScheduler">The task scheduler instance.</param>
        public void SetTaskScheduler(TaskScheduler taskScheduler)
        {
            _taskScheduler = taskScheduler;
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => HandleRequests(_cancellationTokenSource.Token));
            Console.WriteLine($"[CommandsExtension] Web UI started at http://localhost:{_port}/");
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            Console.WriteLine("[CommandsExtension] Web server stopped.");
        }

        private async Task HandleRequests(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context), cancellationToken);
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"[CommandsExtension] Error: {ex.Message}");
                    }
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string responseString = "";

                if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/")
                {
                    responseString = WebUI.GetHTML();
                    response.ContentType = "text/html";
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/api/commands")
                {
                    responseString = GetCommands();
                    response.ContentType = "application/json";
                }
                else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/api/commands")
                {
                    responseString = AddCommand(request);
                    response.ContentType = "application/json";
                }
                else if (request.HttpMethod == "DELETE" && request.Url.AbsolutePath.StartsWith("/api/commands/"))
                {
                    var id = request.Url.AbsolutePath.Replace("/api/commands/", "");
                    responseString = DeleteCommand(id);
                    response.ContentType = "application/json";
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/api/tasks")
                {
                    responseString = GetScheduledTasks();
                    response.ContentType = "application/json";
                }
                else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/api/tasks")
                {
                    responseString = AddScheduledTask(request);
                    response.ContentType = "application/json";
                }
                else if (request.HttpMethod == "DELETE" && request.Url.AbsolutePath.StartsWith("/api/tasks/"))
                {
                    var id = request.Url.AbsolutePath.Replace("/api/tasks/", "");
                    responseString = DeleteScheduledTask(id);
                    response.ContentType = "application/json";
                }
                else
                {
                    response.StatusCode = 404;
                    responseString = "{\"error\": \"Not found\"}";
                    response.ContentType = "application/json";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                byte[] errorBuffer = Encoding.UTF8.GetBytes($"{{\"error\": \"{ex.Message}\"}}");
                response.ContentLength64 = errorBuffer.Length;
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                response.OutputStream.Close();
            }
        }

        private string GetCommands()
        {
            var commands = new System.Collections.Generic.List<string>();
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, CommandName, Response, RequiredRole, UserCooldownSeconds, GlobalCooldownSeconds, IsEnabled FROM CustomCommands";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        commands.Add($"{{\"id\":{reader.GetInt32(0)},\"commandName\":\"{reader.GetString(1)}\",\"response\":\"{EscapeJson(reader.GetString(2))}\",\"requiredRole\":\"{reader.GetString(3)}\",\"userCooldown\":{reader.GetInt32(4)},\"globalCooldown\":{reader.GetInt32(5)},\"isEnabled\":{reader.GetBoolean(6).ToString().ToLower()}}}");
                    }
                }
            }
            return $"[{string.Join(",", commands)}]";
        }

        private string AddCommand(HttpListenerRequest request)
        {
            using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(body);

                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.GetConnectionString()))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO CustomCommands (CommandName, Response, RequiredRole, UserCooldownSeconds, GlobalCooldownSeconds, IsEnabled, CreatedAt, UpdatedAt)
                        VALUES (@name, @response, @role, @userCooldown, @globalCooldown, @enabled, @now, @now)";

                    command.Parameters.AddWithValue("@name", data["commandName"].ToString());
                    command.Parameters.AddWithValue("@response", data["response"].ToString());
                    command.Parameters.AddWithValue("@role", data.ContainsKey("requiredRole") ? data["requiredRole"].ToString() : "Everyone");
                    command.Parameters.AddWithValue("@userCooldown", data.ContainsKey("userCooldown") ? int.Parse(data["userCooldown"].ToString()) : 5);
                    command.Parameters.AddWithValue("@globalCooldown", data.ContainsKey("globalCooldown") ? int.Parse(data["globalCooldown"].ToString()) : 0);
                    command.Parameters.AddWithValue("@enabled", data.ContainsKey("isEnabled") ? bool.Parse(data["isEnabled"].ToString()) : true);
                    command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));

                    command.ExecuteNonQuery();
                }

                // Reload commands to register the new command
                _plugin.ReloadCommands();

                return "{\"success\": true}";
            }
        }

        private string DeleteCommand(string id)
        {
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM CustomCommands WHERE Id = @id";
                command.Parameters.AddWithValue("@id", int.Parse(id));
                command.ExecuteNonQuery();
            }

            // Reload commands to unregister the deleted command
            _plugin.ReloadCommands();

            return "{\"success\": true}";
        }

        private string GetScheduledTasks()
        {
            var tasks = new System.Collections.Generic.List<string>();
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, TaskName, Message, IntervalMinutes, IsEnabled FROM ScheduledTasks";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add($"{{\"id\":{reader.GetInt32(0)},\"taskName\":\"{reader.GetString(1)}\",\"message\":\"{EscapeJson(reader.GetString(2))}\",\"intervalMinutes\":{reader.GetInt32(3)},\"isEnabled\":{reader.GetBoolean(4).ToString().ToLower()}}}");
                    }
                }
            }
            return $"[{string.Join(",", tasks)}]";
        }

        private string AddScheduledTask(HttpListenerRequest request)
        {
            using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(body);

                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.GetConnectionString()))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO ScheduledTasks (TaskName, Message, IntervalMinutes, IsEnabled, CreatedAt, UpdatedAt)
                        VALUES (@name, @message, @interval, @enabled, @now, @now)";

                    command.Parameters.AddWithValue("@name", data["taskName"].ToString());
                    command.Parameters.AddWithValue("@message", data["message"].ToString());
                    command.Parameters.AddWithValue("@interval", int.Parse(data["intervalMinutes"].ToString()));
                    command.Parameters.AddWithValue("@enabled", data.ContainsKey("isEnabled") ? bool.Parse(data["isEnabled"].ToString()) : true);
                    command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));

                    command.ExecuteNonQuery();
                }

                // Reload scheduled tasks to start the new task
                _taskScheduler?.ReloadTasks();

                return "{\"success\": true}";
            }
        }

        private string DeleteScheduledTask(string id)
        {
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM ScheduledTasks WHERE Id = @id";
                command.Parameters.AddWithValue("@id", int.Parse(id));
                command.ExecuteNonQuery();
            }

            // Reload scheduled tasks to stop the deleted task
            _taskScheduler?.ReloadTasks();

            return "{\"success\": true}";
        }

        private string EscapeJson(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
