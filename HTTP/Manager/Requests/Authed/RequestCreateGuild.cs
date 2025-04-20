using System;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.CSharp;
using WebSocketSharp.Server;
using Microsoft.VisualBasic;
using LegitHttpServer;

public class RequestCreateGuild
{
    public static void Process(OpenCordAddress openCordAddress, string entityBody, HttpResponse response, string ip, HttpRequest request, RegisteredUser user, WebSocketUser wsUser, ZlibStreamContext zlibContext, string[] IDs, string[] numbers)
    {
        if (openCordAddress.createGuildTime == 0)
        {
            openCordAddress.createGuildTime = TimestampUtils.GetTimestamp();
        }
        else
        {
            if (TimestampUtils.GetTimestamp() >= openCordAddress.createGuildTime + 500)
            {
                openCordAddress.createGuildTime = TimestampUtils.GetTimestamp();
                openCordAddress.createGuildRequests = 0;
            }
        }

        if (openCordAddress.createGuildRateLimit != -1)
        {
            if (TimestampUtils.GetTimestamp() >= openCordAddress.createGuildRateLimit)
            {
                openCordAddress.createGuildRateLimit = -1;
                openCordAddress.createGuildRequests = 1;
            }
            else
            {
                openCordAddress.createGuildRequests++;
                long remainingSeconds = (openCordAddress.createGuildRateLimit - TimestampUtils.GetTimestamp());
                Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"You are being rate limited.\",\"retry_after\":" + remainingSeconds + "}", 429, "Too Many Requests");

                if (openCordAddress.createGuildRequests > 8)
                {
                    PunishManager.BanIPAddress(ip);
                }

                return;
            }
        }
        else
        {
            openCordAddress.createGuildRateLimit = -1;
            openCordAddress.createGuildRequests++;

            if (openCordAddress.createGuildRequests > 1)
            {
                openCordAddress.createGuildRateLimit = TimestampUtils.GetTimestamp() + 500;
                Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"You are being rate limited.\",\"retry_after\":10}", 429, "Too Many Requests");
                return;
            }
        }

        if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "name", "icon", "channels", "system_channel_id", "guild_template_code" }))
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.BanUser(user, wsUser);
            return;
        }

        dynamic jss = JObject.Parse(entityBody);

        if (jss.icon != null || jss.system_channel_id != null || jss.guild_template_code != "2TffvPucqHkN")
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.BanUser(user, wsUser);
            return;
        }

        string name = jss.name;

        if (name.Length > 100)
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.BanUser(user, wsUser);
            return;
        }

        string guildID = Utils.GetNewID(), administratorID = Utils.GetNewID(), memberID = Utils.GetNewID(), textChannelsID = Utils.GetNewID(), generalID = Utils.GetNewID(), voiceChannelsID = Utils.GetNewID(), voice1_ID = Utils.GetNewID(), voice2_ID = Utils.GetNewID(), voice3_ID = Utils.GetNewID(), unlimitedID = Utils.GetNewID();
        System.IO.File.WriteAllText("data\\channels\\" + generalID + ".json", "[]");

        GuildChannelLocation generalLocation = new GuildChannelLocation();
        generalLocation.channelID = generalID;
        generalLocation.guildID = guildID;

        DiscordGuild guild = new DiscordGuild();
        guild.afk_timeout = 300;
        guild.id = guildID;
        guild.joined_at = TimestampUtils.GetDiscordTime();
        guild.large = false;
        guild.max_members = 250000;
        guild.max_video_channel_users = 25;
        guild.member_count = 1;
        guild.name = name;
        guild.owner_id = user.id;
        guild.region = "deprecated";
        guild.roles = new List<GuildRole>();
        guild.channels = new List<object>();
        guild.firstMember = new GuildMember();

        GuildCategoryChannel textChannels = new GuildCategoryChannel();
        textChannels.id = textChannelsID;
        textChannels.name = "text channels";
        guild.channels.Add(textChannels);

        GuildTextChannel generalChannel = new GuildTextChannel();
        generalChannel.id = generalID;
        generalChannel.name = "general";
        generalChannel.parent_id = textChannelsID;
        guild.channels.Add(generalChannel);

        GuildCategoryChannel voiceChannels = new GuildCategoryChannel();
        voiceChannels.id = voiceChannelsID;
        voiceChannels.name = "voice channels";
        voiceChannels.position = 1;
        guild.channels.Add(voiceChannels);

        GuildVoiceChannel voice1 = new GuildVoiceChannel();
        voice1.id = voice1_ID;
        voice1.user_limit = 5;
        voice1.name = "Voice #1";
        voice1.parent_id = voiceChannelsID;
        guild.channels.Add(voice1);

        GuildVoiceChannel voice2 = new GuildVoiceChannel();
        voice2.id = voice2_ID;
        voice2.user_limit = 10;
        voice2.name = "Voice #1";
        voice2.position = 1;
        voice2.parent_id = voiceChannelsID;
        guild.channels.Add(voice2);

        GuildVoiceChannel voice3 = new GuildVoiceChannel();
        voice3.id = voice3_ID;
        voice3.user_limit = 15;
        voice3.name = "Voice #3";
        voice3.position = 2;
        voice3.parent_id = voiceChannelsID;
        guild.channels.Add(voice3);

        GuildVoiceChannel unlimited = new GuildVoiceChannel();
        unlimited.id = unlimitedID;
        unlimited.name = "Unlimited";
        unlimited.position = 3;
        unlimited.parent_id = voiceChannelsID;
        guild.channels.Add(unlimited);

        guild.firstMember.user = new GuildUser();
        guild.firstMember.user.avatar = user.avatar;
        guild.firstMember.user.discriminator = user.discriminator;
        guild.firstMember.user.id = user.id;
        guild.firstMember.user.username = user.username;
        guild.firstMember.roles = new List<string>() { administratorID };

        GuildRole everyoneRole = new GuildRole();
        everyoneRole.unicode_emoji = null;
        everyoneRole.position = 0;
        everyoneRole.permissions = "1071698660929";
        everyoneRole.name = "@everyone";
        everyoneRole.mentionable = false;
        everyoneRole.managed = false;
        everyoneRole.id = guildID;
        everyoneRole.icon = null;
        everyoneRole.hoist = false;
        everyoneRole.color = 0;
        guild.roles.Add(everyoneRole);

        GuildRole administratorRole = new GuildRole();
        administratorRole.unicode_emoji = null;
        administratorRole.position = 2;
        administratorRole.permissions = "2199022731263";
        administratorRole.name = "Administrator";
        administratorRole.mentionable = false;
        administratorRole.managed = false;
        administratorRole.id = administratorID;
        administratorRole.icon = null;
        administratorRole.hoist = true;
        administratorRole.color = 14162715;
        guild.roles.Add(administratorRole);

        GuildRole memberRole = new GuildRole();
        memberRole.unicode_emoji = null;
        memberRole.position = 1;
        memberRole.permissions = "1071698660929";
        memberRole.name = "Member";
        memberRole.mentionable = false;
        memberRole.managed = false;
        memberRole.id = memberID;
        memberRole.icon = null;
        memberRole.hoist = true;
        memberRole.color = 1752220;
        guild.roles.Add(memberRole);
        HTTPManager.locations.Add(generalLocation);
        guild.guildMembers.Add(user.id);
        HTTPManager.guilds.Add(guild);

        DataManager.SaveLocations();
        DataManager.SaveGuild(guild);

        string guildData = Utils.GetGuildData(guild);
        Utils.InjectResponse(request, response, "{}", 201, "Created");
        HTTPManager.wsManager.SendTo(zlibContext.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"GUILD_CREATE\",\"s\":3,\"op\":0,\"d\":" + guildData + "}")), wsUser.id);
    }
}