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
using DSharpPlus.Net.Models;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;

namespace TimeGateMarshal
{
    public sealed class Program
    {
        public static DiscordClient Client { get; set; }
        public static CommandsNextExtension Commands { get; set; }
        public static List<ulong> FlaggedUsers { get; set; } = new List<ulong>();

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

            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

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
            var timer = new Timer(10000); // 1 min interval
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
                        await DeleteAllMessagesInChannel(Client, 1144339837969780847);
                        await LockChannel(Client, 1144339837969780847); // Change to your channel ID
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

            // Register the MessageCreated event handler
            Client.MessageCreated += HandleMessage;


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
                var discordMod = channel.Guild.GetRole(965991183820152852);

                var overwriteBuilder = new DiscordOverwriteBuilder[] {new DiscordOverwriteBuilder(everyoneRole)
                    .Deny(Permissions.AccessChannels)};

                var overwriteBuilder1 = new DiscordOverwriteBuilder[] {new DiscordOverwriteBuilder(preimerSeries)
                    .Deny(Permissions.SendMessages)
                    .Deny(Permissions.ReadMessageHistory)};

                var overwriteBuilder3 = new DiscordOverwriteBuilder[] {new DiscordOverwriteBuilder(discordMod)
                    .Allow(Permissions.All)};

                //Modify the channel's permissions with the new overwrite
                //await channel.AddOverwriteAsync(everyoneRole, Permissions.None);
                await channel.AddOverwriteAsync(preimerSeries, Permissions.AccessChannels);
                await channel.AddOverwriteAsync(discordMod, Permissions.All);



                //await channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilder1);
                await channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilder);
                //await channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilder3);

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
                var everyoneRole = channel.Guild.GetRole(726995505623728148);
                var discordMod = channel.Guild.GetRole(965991183820152852);

                //var overwriteBuilder = new DiscordOverwriteBuilder[] {new DiscordOverwriteBuilder(everyoneRole)
                //    .Deny(Permissions.SendMessages)
                //    .Deny(Permissions.ReadMessageHistory)};

                //await channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilder);
                //await channel.AddOverwriteAsync(everyoneRole, Permissions.None);
                await channel.AddOverwriteAsync(preimerSeries, Permissions.SendMessages | Permissions.ReadMessageHistory | Permissions.AccessChannels);
                await channel.AddOverwriteAsync(discordMod, Permissions.All);
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

        static async Task HandleMessage(DiscordClient sender, MessageCreateEventArgs e)
        {
            // List of banned words or phrases
            var bannedWords = new List<string> { "fuck", "fUck", "fuCk", "fucK", "FucK", "FUcK", "FuCK", "FUCK",
                "shit", "shIt", "shiT", "sHit", "sHIt", "sHIi", "sHIT", "SHIT",
                "bitch", "biTch", "biTCh", "biTCj", "bitCh", "bitCH", "bitCjH", "bitch",
                "asshole", "asShole", "asShoLe", "asSholE", "Asshole", "AsShole", "AsShoLe", "AsSholE",
                "dick", "diCk", "diCk", "dicK", "Dick", "DiCk", "DiCk", "DiCk",
                "pussy", "puSsy", "pusSy", "pussY", "Pussy", "PuSsy", "PusSy", "PussY",
                "cock", "coCk", "coCk", "cocK", "Cock", "CoCk", "CoCk", "CoCk",
                "cunt", "cuNt", "cuNt", "cunT", "Cunt", "CuNt", "CuNt", "CunT",
                "nigger", "nigGer", "nigGer", "niggEr", "niggeR", "Nigger", "NigGer", "NigGer", "NiggEr", "NiggeR",
                "faggot", "faGgot", "faGgot", "fagGot", "faggOt", "faggoT", "Faggot", "FaGgot", "FaGgot", "FagGot", "FaggOt", "FaggOT" };

            // Check is the message contains any banned words
            if (ContainsBannedWord(e.Message.Content, bannedWords))
            {
                // Flag the user by storing their user ID
                FlaggedUsers.Add(e.Author.Id);

                // Timeout the user for a specified duration
                var timeoutDuration = TimeSpan.FromSeconds(15);

                // Get the DiscordMember object for the user
                var member = await e.Guild.GetMemberAsync(e.Author.Id);

                // Apply the timeout
                await member.TimeoutAsync(DateTimeOffset.UtcNow.Add(timeoutDuration));

                // Delete the message
                await e.Message.DeleteAsync();

                // Send a warning message
                await e.Channel.SendMessageAsync($"User {e.Author.Mention} has been timed out for using banned words.");
            }
        }

        static bool ContainsBannedWord(string messageContent, List<string> bannedWords)
        {
            foreach (var word in bannedWords)
            {
                if (messageContent.Contains(word))
                    return true;
            }
            return false;
        }
    }
}
