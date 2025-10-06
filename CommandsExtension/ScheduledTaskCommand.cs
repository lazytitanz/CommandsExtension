using System;
using TwitchChatBot;
using TwitchLib.Client;

namespace CommandsExtension
{
    /// <summary>
    /// Represents a scheduled task that sends a message to Twitch chat at regular intervals.
    /// Implements IScheduledCommand to work with the bot's built-in scheduler.
    /// </summary>
    internal class ScheduledTaskCommand : IScheduledCommand
    {
        private readonly string _message;

        /// <summary>
        /// Creates a new scheduled task command.
        /// </summary>
        /// <param name="message">The message to send when the task executes.</param>
        public ScheduledTaskCommand(string message)
        {
            _message = message;
        }

        /// <summary>
        /// Executes the scheduled task by sending the configured message to chat.
        /// </summary>
        /// <param name="client">The Twitch client.</param>
        /// <param name="channel">The channel to send the message to.</param>
        void IScheduledCommand.Execute(TwitchClient client, string channel)
        {
            client.SendMessage(channel, _message);
        }
    }
}
