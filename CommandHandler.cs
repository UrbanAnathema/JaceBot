using System.Threading.Tasks;
using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;
using Jace.Modules.Public;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System;
using Discord;

namespace JaceBot
{
    public class CommandHandler
    {
        private CommandService commands;
        private DiscordSocketClient _client;
        private IDependencyMap map;
        public async Task Install(IDependencyMap _map)
        {
            _client = _map.Get<DiscordSocketClient>();
            commands = new CommandService();
            _map.Add(commands);
            map = _map;
            await commands.AddModuleAsync<PublicModule>();
            _client.MessageReceived += HandleCommand;
        }
        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;
            // Mark where the prefix ends and the command begins
            int argPos = 0;
            //
            // This section is for determining if a card has been searched, and then returning the result.
            //

            string cardReg = @"\[\[(.*?)\]\]";
            if (Regex.IsMatch(message.Content, cardReg, RegexOptions.Compiled))
            {
                var cardMatches = Regex.Matches(message.Content, cardReg, RegexOptions.Compiled).Cast<Match>().ToList();
                if (cardMatches.Count > 6)
                {
                    await message.Channel.SendMessageAsync($"**ERROR:** Can not search for more than 6 cards at a time!");
                    return;
                }
                var cardNames = cardMatches.Select(x => x.Groups[1].Value).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
                foreach (var cardName in cardNames)
                {
                    List<Card> CardResults = CardService.searchCard(cardName);
                    if (CardResults.Count <= 0)
                        await message.Channel.SendMessageAsync("**ERROR**:\nYour search resulted in nothing, noob!");
                    else if (CardResults.Count > 6)
                        await message.Channel.SendMessageAsync("**ERROR**:\nToo many results for your search: " + cardName + "! Please refine it");
                    else
                        foreach (Card card in CardResults)
                        {
                            Embed embeded = CreateInfoEmbed(card, message);
                            CardService.lastCard = card;
                            await message.Channel.SendMessageAsync("", embed: embeded);
                        }
                }
            }

            //
            //
            //

            // Determine if the message has a valid prefix, adjust argPos
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('~', ref argPos)))
                return;

            // Create a command context
            var context = new SocketCommandContext(_client, message);

            //Execute the command, store the result
            var result = await commands.ExecuteAsync(context, argPos, map);

            //If the command failed, notify the user
            if (!result.IsSuccess)
                await message.Channel.SendMessageAsync($"**ERROR:** {result.ErrorReason}");
        }

        // These methods below are all used in returning the information about the card
        private static Embed CreateInfoEmbed(Card card, SocketUserMessage message)
        {
            // have to add checks to either return the formatting for walkers, lands, and creatures
            string description = "";
            if (!(string.IsNullOrEmpty(card.Loyalty)))
                description = $"{card.Type}\n{card.Text}\nLoyalty: [{card.Loyalty}]";
            else if (!(string.IsNullOrEmpty(card.PT)))
                description = $"{card.Type}\n{card.Text}\nPower / Toughness: [{card.PT}]";
            else
                description = $"{card.Type}\n{card.Text}";

            
            EmbedBuilder embeded = new EmbedBuilder()
                .WithTitle($"{card.Name} ({card.ManaCost})")
                .WithUrl(CardService.GetGathererLink(card))
                .WithColor(CardService.GetCardColor(card))
                .WithDescription(description)
                .WithThumbnailUrl(CardService.GetCardImage(card));
            return embeded;
        }
    }
}
