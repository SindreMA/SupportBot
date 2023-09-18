
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
namespace Support
{
   public class Program
    {
        static void Main(string[] args)
            => new Program().StartAsync().GetAwaiter().GetResult();
        private CommandHandler _handler;
        private DiscordSocketClient _client;

        public class JoinInfo
        {
            public DateTime Joined { get; set; }
            public ulong User { get; set; }
            public string problem { get; set; }
        }
        public class ChannelQueue
        {
            public ulong ChannelID { get; set; }
            public List<JoinInfo> UserQueue { get; set; }
        }
        public class MessageInfo
        {
            public ulong TextChannelID { get; set; }
            public ulong VoiceChannelID { get; set; }
            public ulong Message { get; set; }
        }
        public class GlobalSettins
        {
            public string Token { get; set; }
            public List<settings> GuildSettings { get; set; }
        }
        public class settings
        {
            public ulong GuildID { get; set; }
            public List<ChannelQueue> ChannelQueues { get; set; }
            public List<MessageInfo> Messages { get; set; }
        }
        public static string filename = "settings.json";
        public async Task StartAsync()
        {
            try
            {

                if (File.Exists(filename))
                {
                    var gs = JsonConvert.DeserializeObject<GlobalSettins>(File.ReadAllText(filename));
                    _settings = gs.GuildSettings;
                    token = gs.Token;
                }
                else
                {
                    File.WriteAllText(filename, JsonConvert.SerializeObject(new GlobalSettins() { Token = "YOUR TOKEN HERE", GuildSettings = new List<settings>() }, Formatting.Indented));
                    throw new Exception("Settings file created at " + filename + " add your token and try again");
                }

                Log("Starting up the bot", ConsoleColor.Green);

                _client = new DiscordSocketClient();
                new CommandHandler(_client);
                new ListUpdater(_client);
                Log("Logging in...", ConsoleColor.Green);
                await _client.LoginAsync(TokenType.Bot, token);
                Log("Connecting...", ConsoleColor.Green);
                _client.GuildMembersDownloaded += _client_GuildMembersDownloaded;

                await _client.StartAsync();
                await Task.Delay(-1);
                _handler = new CommandHandler(_client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }

        }

        private Task _client_GuildMembersDownloaded(SocketGuild arg)
        {
            Log(arg.Name + " connected!", ConsoleColor.Green);
            Thread.Sleep(5000);
            var settings = ListUpdater.GetSettings(arg);
            foreach (var guildSettings in _settings)
            {
                foreach (var ChannelQueue in guildSettings.ChannelQueues)
                {
                    try
                    {
                        var vchannel = arg.GetChannel(ChannelQueue.ChannelID);
                        foreach (var userQueue in ChannelQueue.UserQueue.ToList())
                        {
                            if (!vchannel.Users.ToList().Exists(x => x.Id == userQueue.User))
                            {
                                ChannelQueue.UserQueue.Remove(userQueue);
                            }
                        }
                        //ListUpdater.RefreshLists(ChannelQueue.ChannelID, settings, ChannelQueue, arg, vchannel);
                    }
                    catch (Exception ex)
                    {
                        Log("In startup", ConsoleColor.Red);
                        Log(ex.Message, ConsoleColor.Red);

                    }

                }
            }
            return null;
        }
        public static List<settings> _settings = new List<settings>();
        public static string location = System.AppDomain.CurrentDomain.BaseDirectory;
        public static string token = "";

        public static void Log(string message, ConsoleColor color)
        {
            File.AppendAllText(location + "\\log.txt", DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " : " + message + Environment.NewLine);
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.Now + " : " + message, color);
            Console.ResetColor();
        }

    }


}
