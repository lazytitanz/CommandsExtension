using System;
using TwitchChatBot;
using TwitchLib.Client;
using TwitchLib.Client.Events;

namespace CommandsExtension
{
    /// <summary>
    /// Represents a dynamically created chat command loaded from the database.
    /// Sends a static response message when executed.
    /// </summary>
    public class DynamicCommand : ICommand
    {
        private readonly string _response;
        private readonly Role _requiredRole;
        private readonly int _userCooldownSeconds;
        private readonly int _globalCooldownSeconds;

        /// <summary>
        /// Creates a new dynamic command with the specified parameters.
        /// </summary>
        /// <param name="response">The message to send when the command is executed.</param>
        /// <param name="requiredRole">The minimum role required to use this command.</param>
        /// <param name="userCooldown">Per-user cooldown in seconds.</param>
        /// <param name="globalCooldown">Global cooldown in seconds.</param>
        public DynamicCommand(string response, string requiredRole, int userCooldown, int globalCooldown)
        {
            _response = response;
            _requiredRole = ParseRole(requiredRole);
            _userCooldownSeconds = userCooldown;
            _globalCooldownSeconds = globalCooldown;
        }

        Role ICommand.RequiredRole => _requiredRole;
        int ICommand.UserCooldownSeconds => _userCooldownSeconds;
        int ICommand.GlobalCooldownSeconds => _globalCooldownSeconds;

        void ICommand.Execute(TwitchClient client, OnMessageReceivedArgs e, string[] args)
        {
            client.SendMessage(e.ChatMessage.Channel, _response);
        }

        /// <summary>
        /// Parses a role string into the corresponding Role enum value.
        /// </summary>
        /// <param name="role">The role string (case-insensitive).</param>
        /// <returns>The corresponding Role enum value, defaulting to Everyone if not recognized.</returns>
        private Role ParseRole(string role)
        {
            return role?.ToLower() switch
            {
                "broadcaster" => Role.Broadcaster,
                "moderator" => Role.Moderator,
                "vip" => Role.Vip,
                "subscriber" => Role.Subscriber,
                _ => Role.Everyone
            };
        }
    }
}
