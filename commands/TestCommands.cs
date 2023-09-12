using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TimeGateMarshal.commands
{
    public class TestCommands : BaseCommandModule
    {
        [Command("test")]
        public async Task MyFirstCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Hello");
        }

        [Command("embed")]
        public async Task EmbedMessage(CommandContext ctx)
        {
            ulong chosenUserID = 708507332291854438;
            ulong theChosenOne = 248569861356126209;

            var user = await ctx.Client.GetUserAsync(chosenUserID);
            var user1 = await ctx.Client.GetUserAsync(theChosenOne);

            if (ctx.User.Username != user.Username)
            {
                var message = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.User.Username} wanted everyone to know that",
                    Description = $"{ctx.User.Username} is better than {user.Username}",
                    Color = DiscordColor.Azure
                };
                await ctx.Channel.SendMessageAsync(message);
            }
            else
            {
                var message = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.User.Username} wanted everyone to know that",
                    Description = $"{ctx.User.Username} is worse than {user1.Username}",
                    Color = DiscordColor.Azure
                };
                await ctx.Channel.SendMessageAsync(message);
            }

        }

        [Command("interactivity")]
        public async Task InteractivityTest(CommandContext ctx)
        {
            var interactiviy = Program.Client.GetInteractivity();

            var messageToRetrieve = await interactiviy.WaitForMessageAsync(message => message.Content == "Hello");

            if (messageToRetrieve.Result.Content == "Hello")
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Username} said Hello");
            }
        }

    }
}
