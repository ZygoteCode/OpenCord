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

public class RequestGetMessages
{
    public static void Process(OpenCordAddress openCordAddress, string entityBody, HttpResponse response, string ip, HttpRequest request, RegisteredUser user, WebSocketUser wsUser, ZlibStreamContext zlibContext, string[] IDs, string[] numbers)
    {
        if (openCordAddress.getMessagesTime == 0)
        {
            openCordAddress.getMessagesTime = TimestampUtils.GetTimestamp();
        }
        else
        {
            if (TimestampUtils.GetTimestamp() >= openCordAddress.getMessagesTime + 60)
            {
                openCordAddress.getMessagesTime = TimestampUtils.GetTimestamp();
                openCordAddress.getMessagesRequests = 0;
            }
        }

        if (openCordAddress.getMessagesRateLimit != -1)
        {
            if (TimestampUtils.GetTimestamp() >= openCordAddress.getMessagesRateLimit)
            {
                openCordAddress.getMessagesRateLimit = -1;
                openCordAddress.getMessagesRequests = 1;
            }
            else
            {
                openCordAddress.getMessagesRequests++;
                long remainingSeconds = (openCordAddress.getMessagesRateLimit - TimestampUtils.GetTimestamp());
                Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"You are being rate limited.\",\"retry_after\":" + remainingSeconds + "}", 429, "Too Many Requests");

                if (openCordAddress.getMessagesRequests > 15)
                {
                    PunishManager.BanIPAddress(ip);
                }

                return;
            }
        }
        else
        {
            openCordAddress.getMessagesRateLimit = -1;
            openCordAddress.getMessagesRequests++;

            if (openCordAddress.getMessagesRequests > 15)
            {
                openCordAddress.getMessagesRateLimit = TimestampUtils.GetTimestamp() + 60;
                Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"You are being rate limited.\",\"retry_after\":10}", 429, "Too Many Requests");
                return;
            }
        }

        string channelID = IDs[0];

        if (numbers[0] != "50")
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.BanUser(user, wsUser);
            return;
        }

        foreach (GuildChannelLocation location in HTTPManager.locations)
        {
            if (location.channelID.Equals(channelID))
            {
                foreach (string member in JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("data\\guilds\\" + location.guildID + "\\members.json")))
                {
                    if (member.Equals(user.id))
                    {
                        Utils.InjectResponse(request, response, DataManager.GetChannelMessages(channelID), 200, "OK");
                        return;
                    }
                }
            }
        }

        Utils.InjectResponse(request, response, "[]", 200, "OK");
    }
}