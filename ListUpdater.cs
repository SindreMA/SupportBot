using Discord.WebSocket;
using Discord;
using Discord.Commands;
using Discord.Net;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace Support
{
    internal class ListUpdater
    {
        private DiscordSocketClient _client;

        public ListUpdater(DiscordSocketClient client)
        {
            _client = client;
            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
        }

        private Task _client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState _old, SocketVoiceState _new)
        {
            SocketGuild guild = null;
            if (_old.VoiceChannel != null) guild = _old.VoiceChannel.Guild;
            if (_new.VoiceChannel != null) guild = _new.VoiceChannel.Guild;
            var settings = GetSettings(guild);
            //Joins a support channel
            if (_new.VoiceChannel != null)
            {
                var channelq = GetVchannelSettings(settings, _new.VoiceChannel.Id);
                channelq.UserQueue.Add(new Program.JoinInfo() { Joined = DateTime.Now, User = user.Id });
                ValidateList(channelq,_new.VoiceChannel);
                RefreshLists(channelq.ChannelID, settings, channelq, guild, _new.VoiceChannel);
            }
            //Leaves a support channel
            if (_old.VoiceChannel != null)
            {
                bool changed = false;
                var channelq = GetVchannelSettings(settings, _old.VoiceChannel.Id);
                if (channelq.UserQueue.Exists(x => x.User == user.Id))
                {
                    channelq.UserQueue.Remove(channelq.UserQueue.Find(x => x.User == user.Id));
                    RefreshLists(channelq.ChannelID, settings, channelq, guild, _old.VoiceChannel);
                }
            }
            return null;
        }

        private void ValidateList(Program.ChannelQueue channelq, SocketVoiceChannel voiceChannel)
        {
            foreach (var item in channelq.UserQueue)
            {
                if (!voiceChannel.Users.Any(x=> x.Id == item.User))
                {
                    channelq.UserQueue.Remove(item);
                }
            }
        }

        public static Program.ChannelQueue GetVchannelSettings(Program.settings settings, ulong id)
        {
            if (settings.ChannelQueues.Exists(x => x.ChannelID == id))
            {
                return settings.ChannelQueues.Find(x => x.ChannelID == id);
            }
            else
            {
                var channel = new Program.ChannelQueue() { ChannelID = id, UserQueue = new List<Program.JoinInfo>() };
                settings.ChannelQueues.Add(channel);
                return channel;
            }
        }

        public static Program.settings GetSettings(SocketGuild guild)
        {
            if (Program._settings.Exists(x => x.GuildID == guild.Id))
            {
                return Program._settings.Find(x => x.GuildID == guild.Id);
            }
            else
            {
                var settings = new Program.settings() { GuildID = guild.Id, ChannelQueues = new List<Program.ChannelQueue>(), Messages = new List<Program.MessageInfo>() };
                Program._settings.Add(settings);
                return settings;
            }
        }

        public static void RefreshLists(ulong ChannelID, Program.settings Settings, Program.ChannelQueue Queue, SocketGuild guild, SocketVoiceChannel vchannel)
        {
            if (Settings.Messages.Exists(x => x.VoiceChannelID == vchannel.Id))
            {
                foreach (var message in Settings.Messages.Where(x => x.VoiceChannelID == vchannel.Id))
                {
                    try
                    {

                        var UpdatedList = GenerateEmbededMessage(Queue.UserQueue, guild);
                        UpdatedList.Title = $"Kø til support";
                        UpdatedList.Timestamp = DateTime.Now;
                        var embed = UpdatedList.Build();

                        var msg = guild.GetTextChannel(message.TextChannelID).GetMessageAsync(message.Message).Result as IUserMessage;
                        msg.ModifyAsync(x => x.Embed = embed);
                    }
                    catch (Exception ex)
                    {

                        Program.Log("In Refreshlist", ConsoleColor.Red);
                        Program.Log(ex.Message, ConsoleColor.Red);
                    }
                }
            }

        }

        public static EmbedBuilder GenerateEmbededMessage(List<Program.JoinInfo> userQueue, SocketGuild guild)
        {
            int i = 1;
            var eb = new EmbedBuilder();
            //eb.Description = "```";
            foreach (var useritem in userQueue)
            {
                var user = guild.GetUser(useritem.User);
                eb.Description = eb.Description + $"{i}. { user.Nickname ?? user.Username} - [{useritem.Joined.ToString("HH:mm").Replace(".", ":")}] {useritem.problem ?? ""}\n";
                i++;
            }
            //eb.Description = eb.Description + "```";
            if (userQueue.Count == 0) eb.Description = "Channel is empty";
            else
            {
                var f = new EmbedFooterBuilder();
                f.Text = userQueue.Count + " users in channel";
                eb.Footer = f;
            }

            File.WriteAllText(Program.filename, JsonConvert.SerializeObject(new Program.GlobalSettins() { Token = Program.token, GuildSettings = Program._settings }, Formatting.Indented));
            return eb;
        }
        public static Program.JoinInfo GetUserJoinInfo(ulong userid, SocketGuild guild)
        {
            var settings = GetSettings(guild);
            return settings.ChannelQueues.Find(x => x.UserQueue.Exists(c => c.User == userid)).UserQueue.Find(x => x.User == userid);
        }
    }
}