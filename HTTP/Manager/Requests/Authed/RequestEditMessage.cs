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
using LegitHttpServer;

public class RequestEditMessage
{
    public static void Process(OpenCordAddress openCordAddress, string entityBody, HttpResponse response, string ip, HttpRequest request, RegisteredUser user, WebSocketUser wsUser, ZlibStreamContext zlibContext, string[] IDs, string[] numbers)
    {
        if (openCordAddress.sendMessageTime == 0)
        {
            openCordAddress.sendMessageTime = TimestampUtils.GetTimestamp();
        }
        else
        {
            if (TimestampUtils.GetTimestamp() >= openCordAddress.sendMessageTime + 10)
            {
                openCordAddress.sendMessageTime = TimestampUtils.GetTimestamp();
                openCordAddress.sendMessageRequests = 0;
            }
        }

        if (openCordAddress.sendMessageRateLimit != -1)
        {
            if (TimestampUtils.GetTimestamp() >= openCordAddress.sendMessageRateLimit)
            {
                openCordAddress.sendMessageRateLimit = -1;
                openCordAddress.sendMessageRequests = 1;
            }
            else
            {
                openCordAddress.sendMessageRequests++;
                long remainingSeconds = (openCordAddress.sendMessageRateLimit - TimestampUtils.GetTimestamp());
                Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"The write action you are performing on the channel has hit the write rate limit.\",\"retry_after\":" + remainingSeconds + "}", 429, "Too Many Requests");

                if (openCordAddress.sendMessageRequests > 13)
                {
                    PunishManager.BanIPAddress(ip);
                }

                return;
            }
        }
        else
        {
            openCordAddress.sendMessageRateLimit = -1;
            openCordAddress.sendMessageRequests++;

            if (openCordAddress.sendMessageRequests > 10)
            {
                openCordAddress.sendMessageRateLimit = TimestampUtils.GetTimestamp() + 10;
                Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"The write action you are performing on the channel has hit the write rate limit.\",\"retry_after\":10}", 429, "Too Many Requests");
                return;
            }
        }

        if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "content" }))
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.BanUser(user, wsUser);
            return;
        }

        dynamic jss = JObject.Parse(entityBody);

        if (((string)jss.content).Replace(" ", "").Replace('\t'.ToString(), "") == "")
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.BanUser(user, wsUser);
            return;
        }

        if (((string)jss.content).Length > 2000)
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.BanUser(user, wsUser);
            return;
        }

        string channelID = IDs[0], messageID = IDs[1];
        List<string> members = new List<string>();
        string msg = "";
        bool exit = false;
        DiscordMessage message = new DiscordMessage();
        string guildID = "";

        foreach (GuildChannelLocation location in HTTPManager.locations)
        {
            if (location.channelID.Equals(channelID))
            {
                members = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText("data\\guilds\\" + location.guildID + "\\members.json"));

                foreach (string member in members)
                {
                    if (member.Equals(user.id))
                    {
                        guildID = location.guildID;
                        exit = true;
                        message = DataManager.EditMessage(location.channelID, messageID, (string) jss.content, TimestampUtils.GetDiscordTime());
                        msg = JsonConvert.SerializeObject(message);
                        Utils.InjectResponse(request, response, "", 200, "OK");
                        break;
                    }
                }

                if (exit)
                {
                    break;
                }
            }
        }

        if (exit)
        {
            foreach (string member in members)
            {
                foreach (WebSocketUser webSocketUser in OpenCordWS.users)
                {
                    if (webSocketUser.user.id.Equals(member))
                    {
                        wsUser.sequence++;
                        HTTPManager.wsManager.SendTo(webSocketUser.zlibStreamContext.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"MESSAGE_UPDATE\",\"s\":" + wsUser.sequence + ",\"op\":0,\"d\":{\"type\":0," + "\"tts\":" + message.tts.ToString().ToLower() + ",\"timestamp\":\"" + message.timestamp + "\"," + "\"referenced_message\":null,\"pinned\":false,\"nonce\":\"" + message.nonce + "\"," + "\"mentions\":[],\"mention_roles\":[],\"mention_everyone\":false,\"member\":" + "{\"roles\":[],\"mute\":false,\"joined_at\":" + "\"" + TimestampUtils.GetDiscordTime() + "\",\"hoisted_role\":null," + "\"deaf\":false},\"id\":\"" + message.id + "\",\"flags\":" + message.flags + ",\"embeds\":[]," + "\"edited_timestamp\":" + JSONUtils.ParseJsonString(message.edited_timestamp) + ",\"content\":\"" + message.content.Replace("\n", "\\" + "n") + "\",\"components\":[]," + "\"channel_id\":\"" + message.channel_id + "\",\"author\":{\"username\":\"" + user.username + "\"," + "\"public_flags\":0,\"id\":\"" + user.id + "\",\"discriminator\":\"" + user.discriminator + "\"," + "\"avatar\":" + (user.avatar != null ? "\"" + user.avatar + "\"" : "null") + "},\"attachments\":[]," + "\"guild_id\":\"" + guildID + "\"}}")), webSocketUser.id);
                        break;
                    }
                }
            }
        }
        else
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
        }
    }
}