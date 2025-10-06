# CommandsExtension Plugin

A dynamic command management plugin for TwitchChatBot that provides a web-based interface for creating, editing, and deleting custom chat commands in real-time.

## Features

- 🌐 **Web UI** - Manage commands through a browser-based interface
- ⚡ **Hot Reload** - Add/remove commands without restarting the bot
- 💾 **Persistent Storage** - SQLite database for reliable command storage
- 🔒 **Role-Based Permissions** - Set minimum roles (Everyone, Subscriber, VIP, Moderator, Broadcaster)
- ⏱️ **Cooldown Management** - Per-user and global cooldowns
- 📋 **Scheduled Tasks** - Create recurring messages that send automatically at set intervals

## Requirements

- .NET 8.0 or later
- TwitchChatBot host application
- Trusted publisher status (for HTTP server capability)

## Installation

1. Build the plugin:
   ```bash
   dotnet build
   ```

2. The plugin automatically deploys to:
   ```
   TwitchChatBot/bin/Debug/net8.0/Plugins/CommandExtensions/
   ```

3. Add your publisher name to the bot's `trusted-publishers.json`:
   ```json
   {
     "trustedPublishers": ["SilentNades"]
   }
   ```

4. Restart TwitchChatBot to load the plugin

## Configuration

### Web Server Port

Edit `appsettings.json` in the bot's directory:

```json
{
  "Settings": {
    "WebServerPort": 5000
  }
}
```

Default port is `5000` if not specified.

## Usage

### Accessing the Web UI

Once the plugin loads, access the web interface at:

```
http://localhost:5000
```

The URL is displayed in the bot console on startup.

### Managing Commands

**Via Web UI:**
1. Navigate to `http://localhost:5000`
2. Click "Add Command" to create a new command
3. Fill in the command details:
   - **Command Name** - The trigger (without `!` prefix)
   - **Response** - The message to send when triggered
   - **Required Role** - Minimum role to use the command
   - **User Cooldown** - Seconds between uses per user
   - **Global Cooldown** - Seconds between uses by anyone
4. Commands are immediately available in chat

**In Chat:**
Simply type `!<commandname>` to trigger the command (e.g., `!test`)

### Example Commands

| Command | Response | Role | User CD | Global CD |
|---------|----------|------|---------|-----------|
| `!discord` | Join our Discord: discord.gg/example | Everyone | 30 | 0 |
| `!socials` | Follow me on Twitter @username | Everyone | 60 | 0 |
| `!timeout` | User has been timed out! | Moderator | 0 | 5 |

## Database

The plugin creates a SQLite database at:

```
PluginData/commandextensionsplugin.db
```

**Tables:**
- `CustomCommands` - Stores command definitions
- `ScheduledTasks` - Stores scheduled task definitions (name, message, interval)

## Architecture

### Components

- **CommandsExtensionPlugin** - Main plugin entry point, handles initialization and command registration
- **DynamicCommand** - Implements ICommand for dynamically loaded commands
- **ScheduledTaskCommand** - Implements IScheduledCommand for recurring messages
- **TaskScheduler** - Manages scheduled task registration with bot scheduler
- **WebServer** - HTTP server providing REST API and web UI
- **DatabaseHelper** - SQLite database management

### Plugin Security

The plugin declares the following capabilities in `plugin.json`:

- `network: true` - HTTP server communication
- `disk: true` - Database read/write operations
- `apiHosting: true` - Hosting HTTP endpoints (requires trusted publisher)

## Development

### Building

```bash
# Standard build
dotnet build

# Clean build (recommended after interface changes)
dotnet clean && dotnet build
```

### Project Structure

```
CommandsExtension/
├── CommandsExtensionPlugin.cs  # Main plugin class
├── DynamicCommand.cs           # Command implementation
├── ScheduledTaskCommand.cs     # Scheduled task implementation
├── TaskScheduler.cs            # Task scheduler manager
├── WebServer.cs                # HTTP server
├── DatabaseHelper.cs           # SQLite management
├── webUI.cs                    # Web UI HTML
├── appsettings.json           # Configuration
├── plugin.json                # Security manifest
└── SDK-Documentation.html     # TwitchChatBot SDK reference
```

### Important Notes

- The assembly must be named `CommandsExtensionPlugin.dll` (ends with "Plugin")
- Uses explicit interface implementation for VB.NET interop compatibility
- Shared assemblies (TwitchLib, TwitchChatBot) are excluded from plugin output to prevent type conflicts

## Troubleshooting

### Plugin Not Loading

**"No *Plugin.dll found"**
- Ensure assembly name ends with "Plugin" in `.csproj`
- Check plugin is in `Plugins/CommandExtensions/` folder

**"Unable to cast to IBotPlugin"**
- Run `dotnet clean && dotnet build`
- Verify `TwitchChatBot.dll` is NOT in the plugin folder

### Commands Not Working

**"Command doesn't respond in chat"**
- Check command is enabled in database (`IsEnabled = 1`)
- Verify command was registered (check bot console for "Loaded X command(s)")
- Ensure bot is connected to Twitch chat

**"Failed to initialize: apiHosting"**
- Add your publisher name to `trusted-publishers.json` in bot directory
- Restart the bot

### Web UI Not Accessible

- Check the port in `appsettings.json`
- Look for "Web UI: http://localhost:XXXX" in bot console
- Ensure no other service is using the port
- Try accessing `http://127.0.0.1:5000` instead

## API Reference

### REST Endpoints

**GET /api/commands**
- Returns list of all commands

**POST /api/commands**
- Creates a new command
- Body: `{ "commandName": "test", "response": "Hello!", "requiredRole": "Everyone", "userCooldown": 5, "globalCooldown": 0 }`

**DELETE /api/commands/{id}**
- Deletes command by ID

**GET /api/tasks**
- Returns list of all scheduled tasks

**POST /api/tasks**
- Creates a new scheduled task
- Body: `{ "taskName": "Reminder", "message": "Remember to follow!", "intervalMinutes": 30 }`

**DELETE /api/tasks/{id}**
- Deletes scheduled task by ID

## License

This project is provided as-is for use with TwitchChatBot.

## Author

**SilentNades**

## Version

1.0.0
