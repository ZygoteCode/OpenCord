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

public class RequestGetOwnUser
{
    public static void Process(OpenCordAddress openCordAddress, string entityBody, HttpResponse response, string ip, HttpRequest request, RegisteredUser user, WebSocketUser wsUser, ZlibStreamContext zlibContext, string[] IDs, string[] numbers)
    {
        try
        {
            if (openCordAddress.getOwnUserTime == 0)
            {
                openCordAddress.getOwnUserTime = TimestampUtils.GetTimestamp();
            }
            else
            {
                if (TimestampUtils.GetTimestamp() >= openCordAddress.getOwnUserTime + 120)
                {
                    openCordAddress.getOwnUserTime = TimestampUtils.GetTimestamp();
                    openCordAddress.getOwnUserRequests = 0;
                }
            }

            if (openCordAddress.getOwnUserRateLimit != -1)
            {
                if (TimestampUtils.GetTimestamp() >= openCordAddress.getOwnUserRateLimit)
                {
                    openCordAddress.getOwnUserRateLimit = -1;
                    openCordAddress.getOwnUserRequests = 1;
                }
                else
                {
                    openCordAddress.getOwnUserRequests++;
                    long remainingSeconds = (openCordAddress.getOwnUserRateLimit - TimestampUtils.GetTimestamp());
                    Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"You are being rate limited.\",\"retry_after\":" + remainingSeconds + "}", 429, "Too Many Requests");

                    if (openCordAddress.getOwnUserRequests > 6)
                    {
                        PunishManager.BanIPAddress(ip);
                    }

                    return;
                }
            }
            else
            {
                openCordAddress.getOwnUserRateLimit = -1;
                openCordAddress.getOwnUserRequests++;

                if (openCordAddress.getOwnUserRequests > 20)
                {
                    openCordAddress.getOwnUserRateLimit = TimestampUtils.GetTimestamp() + 120;
                    Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"You are being rate limited.\",\"retry_after\":10}", 429, "Too Many Requests");
                    return;
                }
            }

            string builtJson = "";

            if (user.premium)
            {
                builtJson = "{\"connected_accounts\":[],\"premium_guild_since\":null,\"premium_since\":" + user.premium_since.ToString() + ",\"user\":{\"accent_color\":" + (user.accent_color <= 0 ? "null" : user.accent_color.ToString()) + ",\"avatar\":" + (user.avatar != null && user.avatar != "" ? "null" : "\"" + user.avatar + "\"") + ",\"banner\":" + (user.banner != null && user.banner != "" ? "null" : "\"" + user.banner + "\"") + ",\"discriminator\":\"" + user.discriminator + "\",\"flags\":" + user.flags.ToString() + ",\"id\":\"" + user.id + "\",\"public_flags\":" + user.public_flags + ",\"username\":\"" + user.username + "\"}}";
            }
            else
            {
                builtJson = "{\"connected_accounts\":[],\"user\":{\"accent_color\":" + (user.accent_color <= 0 ? "null" : user.accent_color.ToString()) + ",\"avatar\":" + (user.avatar != null && user.avatar != "" ? "null" : "\"" + user.avatar + "\"") + ",\"banner\":" + (user.banner != null && user.banner != "" ? "null" : "\"" + user.banner + "\"") + ",\"discriminator\":\"" + user.discriminator + "\",\"flags\":" + user.flags.ToString() + ",\"id\":\"" + user.id + "\",\"public_flags\":" + user.public_flags + ",\"username\":\"" + user.username + "\"}}";
            }

            Utils.InjectResponse(request, response, builtJson, 200, "OK");
        }
        catch
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.LockUser(user, wsUser);
        }
    }
}