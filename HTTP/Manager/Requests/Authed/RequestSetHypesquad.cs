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

public class RequestSetHypesquad
{
    public static void Process(OpenCordAddress openCordAddress, string entityBody, HttpResponse response, string ip, HttpRequest request, RegisteredUser user, WebSocketUser wsUser, ZlibStreamContext zlibContext, string[] IDs, string[] numbers)
    {
        try
        {
            user.userFlags.hypeSquadRequests++;

            if (user.userFlags.hypeSquadRequests > 20)
            {
                PunishManager.BanUser(openCordAddress, user, wsUser);
                Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                return;
            }

            if (user.hypesquad)
            {
                Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                return;
            }

            dynamic jss = JObject.Parse(entityBody);
            bool isValid = true;

            if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "house_id" }) || (jss.house_id != 1 && jss.house_id != 2 && jss.house_id != 3))
            {
                Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                PunishManager.LockUser(user, wsUser);
                return;
            }

            int house_id = (int)jss.house_id, flagsValue = 64;

            if (house_id == 2)
            {
                flagsValue = 128;
            }
            else if (house_id == 3)
            {
                flagsValue = 256;
            }

            user.flags |= flagsValue;
            user.public_flags |= flagsValue;
            user.hypesquad = true;
            DataManager.SaveUsers();
            string userJson = JsonConvert.SerializeObject(user);
            HTTPManager.wsManager.SendTo(zlibContext.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"USER_UPDATE\",\"s\":4,\"op\":0,\"d\":" + userJson + "}")), wsUser.id);
        }
        catch
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.LockUser(user, wsUser);
        }
    }
}