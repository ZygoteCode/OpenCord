using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Ionic.Zlib;
using System.Linq;
using System.Threading;

public class OpenCordWS : WebSocketBehavior
{
    public static List<WebSocketUser> users = new List<WebSocketUser>();

    protected override void OnOpen()
    {
        ProcessOpen(Sessions, ID, Context.UserEndPoint.Address.ToString());
    }

    public static void ProcessOpen(WebSocketSessionManager Sessions, string ID, string ip)
    {
        HTTPManager.wsManager = Sessions;
        bool exists = false;
        WebSocketUser user = new WebSocketUser();

        user.id = ID;
        user.ip = ip;
        user.zlibStreamContext = new ZlibStreamContext();

        if (HTTPManager.SAME_IP_CHECK)
        {
            foreach (WebSocketUser alreadyUser in users)
            {
                if (alreadyUser.ip == user.ip)
                {
                    exists = true;
                    break;
                }
            }

            if (exists)
            {
                Sessions.CloseSession(ID);
                return;
            }
        }

        bool isUser = false;

        foreach (RegisteredUser registeredUser in HTTPManager.registeredUsers)
        {
            if (registeredUser.ip == user.ip)
            {
                isUser = true;
                break;
            }
        }

        if (!isUser)
        {
            Sessions.CloseSession(ID);
            return;
        }

        users.Add(user);
    }

    protected override void OnClose(CloseEventArgs e)
    {
        RemoveUser(ID);
    }

    public static void RemoveUser(string id)
    {
        WebSocketUser toRemove = null;

        foreach (WebSocketUser user in users)
        {
            if (user.id == id)
            {
                toRemove = user;
                break;
            }
        }

        users.Remove(toRemove);
        HTTPManager.wsManager.CloseSession(id);
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        ProcessMessage(e.Data, ID, Sessions);
    }

    public static void ProcessMessage(string data, string ID, WebSocketSessionManager Sessions)
    {
        WebSocketUser wsUser = null;

        foreach (WebSocketUser webSocketUser in users)
        {
            if (webSocketUser.id == ID)
            {
                wsUser = webSocketUser;
                break;
            }
        }

        try
        {
            ZlibStreamContext context = wsUser.zlibStreamContext;
            dynamic jss = JObject.Parse(data);

            if (jss.op == 2)
            {
                WSOP2.Process(wsUser, ID, jss, Sessions, data, context);
            }
            else
            {
                if (wsUser.logged)
                {
                    if (jss.op == 1)
                    {
                        WSOP1.Process(wsUser, ID, jss, Sessions, data, context);
                    }
                }
            }
        }
        catch
        {

        }
    }
}