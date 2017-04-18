using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using JaceBot;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Jace.Modules.Public
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        CommandService _service;

        public PublicModule(CommandService commands)
        {
            _service = commands;
        }
        
        /* I am still working on the help command
         * I plan to have it functional in the near future
         * */
        [Command("help")]
        [Summary("Returns the help command")]
        public async Task Help([Remainder] string input)
        {
            if (!(string.IsNullOrEmpty(input)))
                await ReplyAsync("**Error: Parameterized `help` is not implemented yet!");
            else
                await ReplyAsync("**BOT COMMANDS**\nHere's a list of my commands:\n~report\n\n~image\n\n~printings\n\n~rulings\n\n~price (not implemented)");
        }

        [Command("setgame")]
        [RequireOwner]
        [Summary("Sets the current game of the bot")]
        public async Task SetGame([Remainder] string input)
        {
            await Context.Client.SetGameAsync(input);
            writeToFile(input, "currentgame.txt");
            await ReplyAsync($"My current game is now set to {input}");
        }

        [Command("invite")]
        [RequireOwner]
        [Summary("Returns the OAuth2 Invite URL of the bot")]
        public async Task Invite()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            await ReplyAsync($"A user with `MANAGE_SERVER` can invite me to your server here: <https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot>");
        }

        [Command("cardupdate")]
        [RequireOwner]
        [Alias("cu")]
        [Summary("Updates the card DB of the bot, only invokable by the owner of the bot")]
        public async Task CardUpdate()
        {
            await ReplyAsync("**U P D A T I N G  C A R D  D A T A B A S E**");
            CardService.update();
            CardService.buildCardList();
            await ReplyAsync("**U P D A T E  C O M P L E T E!**");
        }

        [Command("dbsize")]
        [RequireOwner]
        [Summary("Debug command")]
        public async Task DBSize()
        {
            await ReplyAsync($"Cards: {CardService.cardDB.Count}\nSets: {CardService.setDB.Count}");
        }

        [Command("source")]
        [Summary("Returns the source code for the bot")]
        public async Task Source()
        {
            await ReplyAsync("The source code for the bot can be found here:\n")
        }
        [Command("report")]
        [Summary("Reports issues to the owner of the bot, usually for the MTG search portion of the bot")]
        public async Task Report()
        {
            //TODO make this method PM me the report, that way I can include more information!
            await ReplyAsync($"**REPORT SUBMITTED TO <@83038332770062336> :**\nLast Card: {CardService.lastCard.Name}\nMuID: {CardService.lastCard.CardSet[0].MuID}");
        }

        [Command("image")]
        [Alias("i")]
        [Summary("Returns a full image of the last searched card")]
        public async Task Image([Remainder] string optionalCardSet="")
        {
            await ReplyAsync("", embed: getCardImage(CardService.lastCard, optionalCardSet));
        }

        [Command("printings")]
        [Alias("p")]
        [Summary("Returns a list of all printings of the last searched card")]
        public async Task Printings()
        {
            if (!CardService.lastCard.Type.Contains("Basic"))
                await ReplyAsync("", embed: getCardInfoPrintings(CardService.lastCard));
            else if (CardService.lastCard.Type.Contains("Basic"))
                await ReplyAsync($"**{CardService.lastCard.Name} (Printings)** \n*I will not display the printings of a basic land!*");
        }

        [Command("rulings")]
        [Alias("r")]
        [Summary("Returns a list of all the rulings issued to this card")]
        public async Task Rulings()
        {
            await ReplyAsync("", embed: getCardInfoRulings(CardService.lastCard));
        }

        [Command("price")]
        [Alias("$")]
        [Summary("Returns the price of all printings of the searched card")]
        public async Task Price()
        {
            await ReplyAsync("Error: This has not been implemented yet!");
        }

        /* IF YOU ARE READING THIS:
         * These methods ARE SUPER JANK
         * Please, if you know ways to optimize them T E L L  M E !!!
         * */
        
        /* writeToFile(string text, string destination)
         * This method is used to write text to a specified destination
         * */
        private static void writeToFile(string text, string destination)
        {
            using (StreamWriter file = new StreamWriter(destination))
            {
                file.WriteLineAsync(text);
            }
        }

        /* getCardInfoPrintings(Card card)
         * Using the parsed Card object, this method will return an Embed to be sent in response to the user request
         * */
        private static Embed getCardInfoPrintings(Card card)
        {
            if (card == null)
                return null;
            string output = "";
            List<CardSet> outputList = new List<CardSet>();
            foreach (CardSet set in card.CardSet)
                if (!(set.MuID.Equals("0")))
                {
                    string longname = "";
                    foreach (Set storedSets in CardService.setDB)
                        if (set.Text.Equals(storedSets.Name))
                        {
                            set.LongName = storedSets.LongName;
                            set.Date = DateTime.Parse(storedSets.ReleaseDate);
                            outputList.Add(set);
                        }
                    output += $"- {longname} ({set.Rarity}) - {set.Text}\n";
                }
            output = orderByDate(outputList);
            
            EmbedBuilder embeded = new EmbedBuilder()
                .WithTitle($"{card.Name} (Printings)")
                .WithUrl(CardService.GetGathererLink(card))
                .WithColor(CardService.GetCardColor(card))
                .WithDescription(output)
                .WithThumbnailUrl(CardService.GetCardImage(card));
            return embeded;
        }

        /* orderByDate(List<CardSet> setList)
         * This method is used to order the list of printings by their release date
         * */
        private static string orderByDate(List<CardSet> setList)
        {
            var list = setList.OrderBy(x => x.Date);
            string output = "";
            foreach (CardSet set in list)
                output += $"- {set.LongName} ({set.Rarity}) - {set.Text}\n";
            return output;
        }
        
        // J A N K  A L A R M 
        /* getCardInfoRulings(Card card)
         * Uses the parsed card to obtain the rulings from the official Gatherer page of the card. I plan to migrate this to an API in the near future
         * */
        private static Embed getCardInfoRulings(Card card)
        {
            string muid = "";
            foreach (CardSet set in card.CardSet)
                if (!(set.MuID.Equals("0")))
                    muid = set.MuID;
            string URL = $"http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid={muid}";
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(URL);
            bool isDivExist = doc.DocumentNode.InnerText.Contains("Rulings");
            string output = "";

            if (isDivExist)
            {
                string rulings = doc.DocumentNode.SelectNodes("//*[@id=\"ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_rulingsContainer\"]/table")[0].InnerText;
                string[] ruleArr = rulings.Split('\n');
                List<string> final = ruleArr.Where(text => text.Length > 2).ToList();
                foreach(string line in final)
                {
                    if (!(string.IsNullOrWhiteSpace(line)))
                    {
                        if (isDate(line))
                            output += "\n- **" + line.Trim() + "**";
                        else
                            output += ": " + line.Trim();
                    }
                }
            }
            else
                return RulingsEmbedCreator(card, 2);

            if (output.Length > 2000)
                return RulingsEmbedCreator(card, 1);
            else
                return RulingsEmbedCreator(card, 0, output);
        }

        /* isDate(string input)
         * This method is used to check if the current line being parsed for rulings contains letters and the symbol '/' If it contains letters, return false
         * */
        private static bool isDate(string input)
        {
            if (Regex.Matches(input, @"[a-zA-z]").Count > 0 || !(input.Contains("/")))
                return false;
            return true;
        }

        /* RulingsEmbedCreator(Card card, int type, string desc="")
         * This method is called to create the final Embed object to be returned to the user. 
         * A type input of 1 means the rulings were too long
         * A type input of 2 means the rulings for the card do not exist
         * A type input of anything else just returns the regular rulings
         * */
        private static Embed RulingsEmbedCreator(Card card, int type, string desc="")
        {
            switch(type)
            {
                case 1:
                    desc = "Error, rulings over 2000 characters. Due to text restrictions of Discord, you are going to have to read the rulings yourself.\nThis can be done by clicking the name of the card, which will take you to the official Gatherer page for the card!";
                    break;
                case 2:
                    desc = "There are no rulings for this card!";
                    break;
            }
            EmbedBuilder embeded = new EmbedBuilder()
                .WithTitle($"{card.Name} (Rulings)")
                .WithUrl(CardService.GetGathererLink(card))
                .WithColor(CardService.GetCardColor(card))
                .WithThumbnailUrl(CardService.GetCardImage(card))
                .WithDescription(desc);
            return embeded;
        }

        /* getCardImage(Card card, string OptionalSet="")
         * This method is used to return the link to embed into discord
         * This method allows users to input a specific printing and to have that one be returned. Based on the 3 character abbreviations
         * */
        private static Embed getCardImage(Card card, string OptionalSet = "")
        {
            string URL = "";
            if (OptionalSet.Equals(""))
            {
                string muid = "";
                foreach (CardSet set in card.CardSet)
                    if (!(set.MuID.Equals("0")))
                        muid = set.MuID;
                URL = $"http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={muid}&type=card";
            }
            else
            {
                OptionalSet = OptionalSet.ToUpper();
                string finalid = "";
                foreach (CardSet set in card.CardSet)
                    if (set.Text.Equals(OptionalSet))
                        finalid = set.MuID;
                if (finalid.Equals(""))
                {
                    EmbedBuilder embedError = new EmbedBuilder()
                        .WithTitle($"**{card.Name} (Image)**")
                        .WithUrl(CardService.GetGathererLink(card))
                        .WithColor(CardService.GetCardColor(card))
                        .WithDescription("**ERROR** : Invalid set abbreviation. Use ~printings to get correct abbreviations!")
                        .WithThumbnailUrl(CardService.GetCardImage(card));
                    return embedError;
                }
                URL = $"http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={finalid}&type=card";
            }

            EmbedBuilder embeded = new EmbedBuilder()
                .WithTitle($"**{card.Name} (Image)**")
                .WithUrl(CardService.GetGathererLink(card))
                .WithColor(CardService.GetCardColor(card))
                .WithImageUrl(URL);
            return embeded;
        }
    }
}