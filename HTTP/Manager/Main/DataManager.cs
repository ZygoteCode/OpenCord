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

public class DataManager
{
    public static void SaveUsers()
    {
        while (HTTPManager.usersSemaphore.IsResourceNotAvailable())
        {
            Thread.Sleep(HTTPManager.SEMAPHORE_SLEEP);
        }

        if (HTTPManager.usersSemaphore.LockResource())
        {
            try
            {
                System.IO.File.WriteAllText("data\\users.json", JsonConvert.SerializeObject(HTTPManager.registeredUsers));
            }
            catch
            {

            }

            HTTPManager.usersSemaphore.UnlockResource();
        }
    }

    public static void SaveLocations()
    {
        while (HTTPManager.locationsSemaphore.IsResourceNotAvailable())
        {
            Thread.Sleep(HTTPManager.SEMAPHORE_SLEEP);
        }

        if (HTTPManager.locationsSemaphore.LockResource())
        {
            try
            {
                System.IO.File.WriteAllText("data\\locations.json", JsonConvert.SerializeObject(HTTPManager.locations));
            }
            catch
            {

            }

            HTTPManager.locationsSemaphore.UnlockResource();
        }
    }

    public static void SaveChannel(string id, DiscordMessage message)
    {
        List<DiscordMessage> messages = JsonConvert.DeserializeObject<List<DiscordMessage>>(System.IO.File.ReadAllText("data\\channels\\" + id + ".json"));
        messages.Add(message);
        System.IO.File.WriteAllText("data\\channels\\" + id + ".json", JsonConvert.SerializeObject(messages));
    }

    public static void SaveGuild(DiscordGuild guild)
    {
        while (guild.semaphore.IsResourceNotAvailable())
        {
            Thread.Sleep(HTTPManager.SEMAPHORE_SLEEP);
        }

        if (guild.semaphore.LockResource())
        {
            try
            {
                if (!System.IO.Directory.Exists("data\\guilds\\" + guild.id))
                {
                    System.IO.Directory.CreateDirectory("data\\guilds\\" + guild.id);
                }

                System.IO.File.WriteAllText("data\\guilds\\" + guild.id + "\\data.json", JsonConvert.SerializeObject(guild));
            }
            catch
            {

            }

            List<GuildTextChannel> textChannels = new List<GuildTextChannel>();
            List<GuildVoiceChannel> voiceChannels = new List<GuildVoiceChannel>();
            List<GuildCategoryChannel> categoryChannels = new List<GuildCategoryChannel>();

            foreach (var channel in guild.channels)
            {
                if (channel.GetType() == typeof(GuildTextChannel))
                {
                    GuildTextChannel textChannel = (GuildTextChannel)channel;
                    textChannels.Add(textChannel);
                }
                else if (channel.GetType() == typeof(GuildCategoryChannel))
                {
                    GuildCategoryChannel categoryChannel = (GuildCategoryChannel)channel;
                    categoryChannels.Add(categoryChannel);
                }
                else if (channel.GetType() == typeof(GuildVoiceChannel))
                {
                    GuildVoiceChannel voiceChannel = (GuildVoiceChannel)channel;
                    voiceChannels.Add(voiceChannel);
                }
            }

            try
            {
                System.IO.File.WriteAllText("data\\guilds\\" + guild.id + "\\textChannels.json", JsonConvert.SerializeObject(textChannels));
                System.IO.File.WriteAllText("data\\guilds\\" + guild.id + "\\voiceChannels.json", JsonConvert.SerializeObject(voiceChannels));
                System.IO.File.WriteAllText("data\\guilds\\" + guild.id + "\\categoryChannels.json", JsonConvert.SerializeObject(categoryChannels));
                System.IO.File.WriteAllText("data\\guilds\\" + guild.id + "\\members.json", JsonConvert.SerializeObject(guild.guildMembers));
            }
            catch
            {

            }

            guild.semaphore.UnlockResource();
        }
    }

    public static string GetChannelMessages(string channelID)
    {
        List<DiscordMessage> messages = JsonConvert.DeserializeObject<List<DiscordMessage>>(System.IO.File.ReadAllText("data\\channels\\" + channelID + ".json"));
        messages.Reverse();
        List<DiscordMessage> newMessages = new List<DiscordMessage>();

        for (int i = 0; i < 50; i++)
        {
            if (i >= messages.Count)
            {
                break;
            }

            newMessages.Add(messages[i]);
        }

        return JsonConvert.SerializeObject(newMessages);
    }

    public static DiscordMessage EditMessage(string channelID, string messageID, string content, string edited_timestamp)
    {
        List<DiscordMessage> messages = JsonConvert.DeserializeObject<List<DiscordMessage>>(System.IO.File.ReadAllText("data\\channels\\" + channelID + ".json"));
        DiscordMessage theMessage = null;

        foreach (DiscordMessage message in messages)
        {
            if (message.id.Equals(messageID))
            {
                theMessage = message;
                message.content = content;
                message.edited_timestamp = edited_timestamp;

                break;
            }
        }

        System.IO.File.WriteAllText("data\\channels\\" + channelID + ".json", JsonConvert.SerializeObject(messages));
        return theMessage;
    }

    public static string GetChannelBeforeMessages(string channelID, string messageID)
    {
        List<DiscordMessage> messages = JsonConvert.DeserializeObject<List<DiscordMessage>>(System.IO.File.ReadAllText("data\\channels\\" + channelID + ".json"));
        messages.Reverse();
        List<DiscordMessage> newMessages = new List<DiscordMessage>();

        bool hasCome = false;
        int theMessages = 0;

        foreach (DiscordMessage message in messages)
        {
            if (!hasCome)
            {
                if (message.id.Equals(messageID))
                {
                    hasCome = true;
                }
            }
            else
            {
                theMessages++;
                newMessages.Add(message);

                if (theMessages >= 50)
                {
                    break;
                }
            }
        }

        return JsonConvert.SerializeObject(newMessages);
    }

    public static Tuple<RegisteredUser, WebSocketUser> GetUserByID(string id)
    {
        RegisteredUser user = null;
        WebSocketUser wsUser = null;

        foreach (RegisteredUser registeredUser in HTTPManager.registeredUsers)
        {
            if (registeredUser.id.Equals(id))
            {
                user = registeredUser;
                break;
            }
        }

        foreach (WebSocketUser webSocketUser in OpenCordWS.users)
        {
            if (webSocketUser.user.id == id)
            {
                wsUser = webSocketUser;
                break;
            }
        }

        return new Tuple<RegisteredUser, WebSocketUser>(user, wsUser);
    }
}