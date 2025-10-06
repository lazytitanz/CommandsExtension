namespace CommandsExtension
{
    internal static class WebUI
    {
        public static string GetHTML()
        {
            return """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Commands Extension - Twitch Bot</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
    <style>
        [x-cloak] { display: none !important; }
    </style>
</head>
<body class="bg-gray-900 text-gray-100">
    <div x-data="commandsApp()" x-init="init()" class="min-h-screen">
        <!-- Header -->
        <header class="bg-purple-600 shadow-lg">
            <div class="container mx-auto px-6 py-8">
                <h1 class="text-4xl font-bold">Commands Extension</h1>
                <p class="text-purple-200 mt-2">Manage custom commands and scheduled tasks</p>
            </div>
        </header>

        <!-- Stats Bar -->
        <div class="bg-gray-800 border-b border-gray-700">
            <div class="container mx-auto px-6 py-4">
                <div class="flex justify-around">
                    <div class="text-center">
                        <div class="text-3xl font-bold text-purple-400" x-text="commands.length">0</div>
                        <div class="text-sm text-gray-400">Custom Commands</div>
                    </div>
                    <div class="text-center">
                        <div class="text-3xl font-bold text-green-400" x-text="tasks.length">0</div>
                        <div class="text-sm text-gray-400">Scheduled Tasks</div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Main Content -->
        <main class="container mx-auto px-6 py-8">
            <!-- Custom Commands Section -->
            <div class="bg-gray-800 rounded-lg shadow-lg p-6 mb-8 border border-gray-700">
                <h2 class="text-2xl font-semibold mb-6 text-purple-400">Custom Commands</h2>

                <!-- Add Command Form -->
                <div class="bg-gray-900 rounded-lg p-6 mb-6 border border-gray-700">
                    <h3 class="text-lg font-semibold mb-4 text-gray-300">Add New Command</h3>
                    <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                        <input type="text" x-model="newCommand.name" placeholder="Command Name (e.g., hello)"
                               class="bg-gray-700 border border-gray-600 rounded px-4 py-2 text-gray-100 placeholder-gray-400 focus:outline-none focus:border-purple-500" />
                        <input type="text" x-model="newCommand.response" placeholder="Response"
                               class="bg-gray-700 border border-gray-600 rounded px-4 py-2 text-gray-100 placeholder-gray-400 focus:outline-none focus:border-purple-500" />
                    </div>
                    <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                        <select x-model="newCommand.role" class="bg-gray-700 border border-gray-600 rounded px-4 py-2 text-gray-100 focus:outline-none focus:border-purple-500">
                            <option value="Everyone">Everyone</option>
                            <option value="Subscriber">Subscriber</option>
                            <option value="VIP">VIP</option>
                            <option value="Moderator">Moderator</option>
                            <option value="Broadcaster">Broadcaster</option>
                        </select>
                        <input type="number" x-model="newCommand.userCooldown" placeholder="User Cooldown (sec)"
                               class="bg-gray-700 border border-gray-600 rounded px-4 py-2 text-gray-100 placeholder-gray-400 focus:outline-none focus:border-purple-500" />
                        <input type="number" x-model="newCommand.globalCooldown" placeholder="Global Cooldown (sec)"
                               class="bg-gray-700 border border-gray-600 rounded px-4 py-2 text-gray-100 placeholder-gray-400 focus:outline-none focus:border-purple-500" />
                    </div>
                    <button @click="addCommand()" class="bg-purple-600 hover:bg-purple-700 text-white px-6 py-2 rounded transition-colors">
                        Add Command
                    </button>
                </div>

                <!-- Commands List -->
                <div class="space-y-3">
                    <div x-show="commands.length === 0" class="text-center py-8 text-gray-500">
                        No custom commands yet. Add one above!
                    </div>
                    <template x-for="cmd in commands" :key="cmd.id">
                        <div class="bg-gray-900 rounded-lg p-4 border border-gray-700 hover:border-purple-500 transition-colors">
                            <div class="flex items-start justify-between">
                                <div class="flex-1">
                                    <div class="flex items-center gap-3 mb-2">
                                        <span class="bg-purple-600 text-white px-3 py-1 rounded-full text-sm font-bold" x-text="`!${cmd.commandName}`"></span>
                                        <span class="bg-gray-700 px-3 py-1 rounded-full text-xs text-gray-400" x-text="cmd.requiredRole"></span>
                                    </div>
                                    <p class="text-gray-300 mb-2" x-text="cmd.response"></p>
                                    <div class="flex items-center gap-4 text-xs text-gray-500">
                                        <span>User CD: <span class="text-purple-400" x-text="cmd.userCooldown + 's'"></span></span>
                                        <span class="text-gray-700">•</span>
                                        <span>Global CD: <span class="text-purple-400" x-text="cmd.globalCooldown + 's'"></span></span>
                                    </div>
                                </div>
                                <button @click="deleteCommand(cmd.id)" class="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded transition-colors">
                                    Delete
                                </button>
                            </div>
                        </div>
                    </template>
                </div>
            </div>

            <!-- Scheduled Tasks Section -->
            <div class="bg-gray-800 rounded-lg shadow-lg p-6 border border-gray-700">
                <h2 class="text-2xl font-semibold mb-6 text-green-400">Scheduled Tasks</h2>

                <!-- Add Task Form -->
                <div class="bg-gray-900 rounded-lg p-6 mb-6 border border-gray-700">
                    <h3 class="text-lg font-semibold mb-4 text-gray-300">Add New Task</h3>
                    <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                        <input type="text" x-model="newTask.name" placeholder="Task Name"
                               class="bg-gray-700 border border-gray-600 rounded px-4 py-2 text-gray-100 placeholder-gray-400 focus:outline-none focus:border-green-500" />
                        <input type="text" x-model="newTask.message" placeholder="Message"
                               class="bg-gray-700 border border-gray-600 rounded px-4 py-2 text-gray-100 placeholder-gray-400 focus:outline-none focus:border-green-500" />
                    </div>
                    <div class="mb-4">
                        <input type="number" x-model="newTask.interval" placeholder="Interval (minutes)"
                               class="bg-gray-700 border border-gray-600 rounded px-4 py-2 text-gray-100 placeholder-gray-400 focus:outline-none focus:border-green-500 w-full md:w-1/2" />
                    </div>
                    <button @click="addTask()" class="bg-green-600 hover:bg-green-700 text-white px-6 py-2 rounded transition-colors">
                        Add Task
                    </button>
                </div>

                <!-- Tasks List -->
                <div class="space-y-3">
                    <div x-show="tasks.length === 0" class="text-center py-8 text-gray-500">
                        No scheduled tasks yet. Add one above!
                    </div>
                    <template x-for="task in tasks" :key="task.id">
                        <div class="bg-gray-900 rounded-lg p-4 border border-gray-700 hover:border-green-500 transition-colors">
                            <div class="flex items-start justify-between">
                                <div class="flex-1">
                                    <div class="flex items-center gap-3 mb-2">
                                        <span class="bg-green-600 text-white px-3 py-1 rounded-full text-sm font-bold" x-text="task.taskName"></span>
                                        <span class="bg-gray-700 px-3 py-1 rounded-full text-xs text-gray-400" x-text="`Every ${task.intervalMinutes} min`"></span>
                                    </div>
                                    <p class="text-gray-300" x-text="task.message"></p>
                                </div>
                                <button @click="deleteTask(task.id)" class="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded transition-colors">
                                    Delete
                                </button>
                            </div>
                        </div>
                    </template>
                </div>
            </div>
        </main>
    </div>

    <script>
        function commandsApp() {
            return {
                commands: [],
                tasks: [],
                newCommand: {
                    name: '',
                    response: '',
                    role: 'Everyone',
                    userCooldown: 5,
                    globalCooldown: 0
                },
                newTask: {
                    name: '',
                    message: '',
                    interval: 60
                },

                init() {
                    this.loadCommands();
                    this.loadTasks();
                },

                async loadCommands() {
                    try {
                        const response = await fetch('/api/commands');
                        this.commands = await response.json();
                    } catch (error) {
                        console.error('Failed to load commands:', error);
                    }
                },

                async addCommand() {
                    if (!this.newCommand.name || !this.newCommand.response) {
                        alert('Please fill in command name and response');
                        return;
                    }

                    try {
                        await fetch('/api/commands', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({
                                commandName: this.newCommand.name,
                                response: this.newCommand.response,
                                requiredRole: this.newCommand.role,
                                userCooldown: parseInt(this.newCommand.userCooldown),
                                globalCooldown: parseInt(this.newCommand.globalCooldown),
                                isEnabled: true
                            })
                        });

                        this.newCommand = {
                            name: '',
                            response: '',
                            role: 'Everyone',
                            userCooldown: 5,
                            globalCooldown: 0
                        };
                        this.loadCommands();
                    } catch (error) {
                        console.error('Failed to add command:', error);
                        alert('Failed to add command');
                    }
                },

                async deleteCommand(id) {
                    if (confirm('Are you sure you want to delete this command?')) {
                        try {
                            await fetch(`/api/commands/${id}`, { method: 'DELETE' });
                            this.loadCommands();
                        } catch (error) {
                            console.error('Failed to delete command:', error);
                        }
                    }
                },

                async loadTasks() {
                    try {
                        const response = await fetch('/api/tasks');
                        this.tasks = await response.json();
                    } catch (error) {
                        console.error('Failed to load tasks:', error);
                    }
                },

                async addTask() {
                    if (!this.newTask.name || !this.newTask.message || !this.newTask.interval) {
                        alert('Please fill in all task fields');
                        return;
                    }

                    try {
                        await fetch('/api/tasks', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({
                                taskName: this.newTask.name,
                                message: this.newTask.message,
                                intervalMinutes: parseInt(this.newTask.interval),
                                isEnabled: true
                            })
                        });

                        this.newTask = {
                            name: '',
                            message: '',
                            interval: 60
                        };
                        this.loadTasks();
                    } catch (error) {
                        console.error('Failed to add task:', error);
                        alert('Failed to add task');
                    }
                },

                async deleteTask(id) {
                    if (confirm('Are you sure you want to delete this task?')) {
                        try {
                            await fetch(`/api/tasks/${id}`, { method: 'DELETE' });
                            this.loadTasks();
                        } catch (error) {
                            console.error('Failed to delete task:', error);
                        }
                    }
                }
            }
        }
    </script>
</body>
</html>
""";
        }
    }
}
