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

public class WSOP1
{
    public static void Process(WebSocketUser wsUser, string ID, dynamic jss, WebSocketSessionManager Sessions, string data, ZlibStreamContext context)
    {
        Sessions.SendTo(context.Deflate(Encoding.UTF8.GetBytes("{\"t\":null,\"s\":null,\"op\":11,\"d\":null}")), ID);
    }
}