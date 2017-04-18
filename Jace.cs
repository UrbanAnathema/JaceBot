using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.IO;

namespace JaceBot
{
    public class Jace
    {
        //Convert our sync main to an async main
        public static void Main(string[] args) => new Jace().Start().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandHandler handler;

        public async Task Start()
        {
            Console.WriteLine("[INFO] Loading cards.xml");
            CardService.buildCardList();
            Console.WriteLine("[INFO] Starting StaticBot !");
            //define the DiscordSocketClient
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info
            });

            Console.WriteLine("[INFO] Begin Discord Logging:");
            _client.Log += HandleLog;

            var token = "winkyface";

            //Login and connect to Discord.
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            var map = new DependencyMap();
            map.Add(_client);

            handler = new CommandHandler();
            await handler.Install(map);

            _client.Connected += HandleConnection;

            // Block this program until it is closed.
            await Task.Delay(-1);
        }

        private Task HandleLog(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task HandleConnection()
        {
            string game = "";
            try
            {
                game = File.ReadAllText("currentgame.txt");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("[ERROR] currentgame.txt NOT FOUND");
            }
            await _client.SetGameAsync(game);
        }
    }
}