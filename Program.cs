using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TimeGateMarshal.commands;
using TimeGateMarshal.config;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Threading.Channels;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography.X509Certificates;

namespace TimeGateMarshal
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }

        static async Task Main(string[] args)
        {
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);

            Client.Ready += Client_Ready;

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new String[] { jsonReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.RegisterCommands<TestCommands>();

            bool isChannelLocked = false; // Initialize a flag to track channel locking
            bool isChannelUnlocked = false; // Initialize a flag to track channel unlocking


            // Create a timer to check the time periodically
            var timer = new Timer(60000); // 1 min interval
            timer.Elapsed += async (sender, e) =>
            {
                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Thursday)
                {
                    var currentTime = DateTime.UtcNow;

                    //var lockTime = currentTime.Date.AddHours(19); // testing hours

                    // Lock the channel at 8PM MST (2000)
                    var lockTime = currentTime.Date.AddHours(16); // correct hours for release

                    // Unlock the channel at 7PM MST
                    var unlockTime = currentTime.Date.AddHours(1); // correct hours for release

                    if (currentTime >= lockTime && currentTime > unlockTime && !isChannelLocked)
                    {
                        // Lock the channel
                        await LockChannel(Client, 1144339837969780847); // Change to your channel ID
                        await DeleteAllMessagesInChannel(Client, 1144339837969780847);
                        isChannelLocked = true; // Set the flag to indicate the channel is locked
                        isChannelUnlocked = false;
                    }
                    else if (currentTime >= unlockTime && currentTime < lockTime && !isChannelUnlocked)
                    {
                        // Unlock the channel
                        await UnlockChannel(Client, 1144339837969780847); // Change to your channel ID
                        isChannelUnlocked = true; // Set the flag to indicate the channel is unlocked
                        isChannelLocked = false;
                    }

                    Console.WriteLine(currentTime);
                }
            };

            timer.Start();


            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        static async Task LockChannel(DiscordClient Client, ulong ChannelID)
        {
            Console.WriteLine("LockChannel called"); // Debugging message
            var channel = await Client.GetChannelAsync(ChannelID);

            if (channel != null)
            {
                Console.WriteLine("Channel found"); // Debugging message
                // Adjust permissions to lock the channel.
                var everyoneRole = channel.Guild.GetRole(726995505623728148);
                var preimerSeries = channel.Guild.GetRole(726996405968961577);

                var overwriteBuilder = new DiscordOverwriteBuilder[] {new DiscordOverwriteBuilder(everyoneRole)
                    .Deny(Permissions.SendMessages)
                    .Deny(Permissions.ReadMessageHistory)
                    .Deny(Permissions.AccessChannels)};

                var overwriteBuilder1 = new DiscordOverwriteBuilder[] {new DiscordOverwriteBuilder(everyoneRole)
                    .Deny(Permissions.SendMessages)
                    .Deny(Permissions.ReadMessageHistory)};

                // Modify the channel's permissions with the new overwrite
                await channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilder);
                await channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilder1);

                Console.WriteLine("Channel locked"); // Debugging message
            }

        }


        static async Task UnlockChannel(DiscordClient client, ulong channelId)
        {
            Console.WriteLine("UnLockChannel called"); // Debugging message
            var channel = await client.GetChannelAsync(channelId) as DiscordChannel;

            if (channel != null)
            {
                Console.WriteLine("Channel found"); // Debugging message
                                                    
                // Remove the specific permissions to unlock the channel.
                var preimerSeries = channel.Guild.GetRole(726996405968961577);

                await channel.AddOverwriteAsync(preimerSeries, Permissions.SendMessages | Permissions.ReadMessageHistory);
                Console.WriteLine("Channel unlocked"); // Debugging message
            }
        }

        static async Task DeleteAllMessagesInChannel(DiscordClient Client, ulong channelID)
        {
            Console.WriteLine("Delete called"); // Debugging message
            var channel = await Client.GetChannelAsync(channelID) as DiscordChannel;


            if (channel != null)
            {
                Console.WriteLine("Channel found"); // Debugging message
                var messages = await channel.GetMessagesAsync();
                await channel.DeleteMessagesAsync(messages);
                Console.WriteLine("Channel messages deleted"); // Debugging message
            }
        }
    }
}
