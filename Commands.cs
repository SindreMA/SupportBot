using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DebateBot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("ShowQueue")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ShowQueue(ulong vchannelid)
        {
            var settings = ListUpdater.GetSettings(Context.Guild);
            var vChannelsettings = ListUpdater.GetVchannelSettings(settings, vchannelid);


            var eb = ListUpdater.GenerateEmbededMessage(vChannelsettings.UserQueue, Context.Guild);
            eb.Title = $"Channel overview for {Context.Guild.GetVoiceChannel(vchannelid).Name}";
            eb.Timestamp = DateTime.Now;
            var msg = await Context.Channel.SendMessageAsync("", false, eb.Build());
            settings.Messages.Add(new Program.MessageInfo() { TextChannelID = Context.Channel.Id, VoiceChannelID = vchannelid, Message = msg.Id });
        }
        [Command("support")]
        public async Task support([Remainder]string text)
        {
            try
            {
                var user = ListUpdater.GetUserJoinInfo(Context.User.Id, Context.Guild);
                user.problem = text;
                var settings = ListUpdater.GetSettings(Context.Guild);
                var vchannel = settings.ChannelQueues.Find(x => x.UserQueue.Exists(c => c == user));
                ListUpdater.RefreshLists(vchannel.ChannelID, settings, vchannel, Context.Guild, Context.Guild.GetVoiceChannel(vchannel.ChannelID));
            }
            catch (Exception)
            {
            }
        }


    }
}