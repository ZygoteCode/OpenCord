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
using WebSocketSharp;
using System.Reflection;

internal class HTTPManager
{
    public static string ipAddress = "127.0.0.1";
    public static short httpPort = 9987, wsPort = 9988;
    public static UrlSchema urlSchema = UrlSchema.HTTP;
    public static List<RegisteredUser> registeredUsers = new List<RegisteredUser>();
    public static List<OpenCordAddress> openCordAddresses = new List<OpenCordAddress>();
    public static List<string> bannedIpAddresses = new List<string>();
    public static int MAX_ACCOUNTS_PER_IP = 3;
    public static int MIN_AGE = 13;
    public static WebSocketServer server = new WebSocketServer(wsPort);
    public static WebSocketSessionManager wsManager;
    public static bool NO_TUTORIALS = true;
    public static int API_VERSION = 9;
    public static ResourceSemaphore usersSemaphore = new ResourceSemaphore();
    public static ResourceSemaphore bannedIpsSemaphore = new ResourceSemaphore();
    public static ResourceSemaphore locationsSemaphore = new ResourceSemaphore();
    public static int SEMAPHORE_SLEEP = 10;
    public static List<DiscordGuild> guilds = new List<DiscordGuild>();
    public static List<GuildChannelLocation> locations = new List<GuildChannelLocation>();
    public static bool SAME_IP_CHECK = false;
    public static RequestChecker requestChecker;
    public static bool AUTO_ASSETS_DOWNLOAD = true;
    public static bool CACHE_DATA = false;

    public static string[] allowedHeaders = new string[] { "X-Super-Properties", "X-Discord-Locale", "Content-Type", "Content-Length", "Host", "Connection", "Accept-Encoding", "Origin", "Accept", "X-Debug-Options", "sec-ch-ua", "sec-ch-ua-mobile", "User-Agent", "Sec-Fetch-Site", "Sec-Fetch-Mode", "Sec-Fetch-Dest", "TE", "Alt-Used", "X-Fingerprint", "sec-ch-ua-platform", "Accept-Language", "Authorization", "Proxy-Connection", "Referer", "X-Context-Properties", "X-Failed-Requests" };

    public static List<Tuple<string, string>> allowedNonAuthedRequests = new List<Tuple<string, string>>()
    {
        new Tuple<string, string>("/auth/register", "POST"),
        new Tuple<string, string>("/auth/login", "POST"),
    };

    public static List<Tuple<string, string>> allowedAuthedRequests = new List<Tuple<string, string>>()
    {
        new Tuple<string, string>("/hypesquad/online", "POST"),
        new Tuple<string, string>("/users/%ID%/profile?with_mutual_guilds=false", "GET"),
        new Tuple<string, string>("/users/@me/settings", "PATCH"),
        new Tuple<string, string>("/guilds", "POST"),
        new Tuple<string, string>("/channels/%ID%/messages?limit=%NUM%", "GET"),
        new Tuple<string, string>("/channels/%ID%/messages?before=%ID%&limit=%NUM%", "GET"),
        new Tuple<string, string>("/channels/%ID%/messages", "POST"),
        new Tuple<string, string>("/channels/%ID%/messages/%ID%", "PATCH")
    };

    public static List<Tuple<string, string>> originRequests = new List<Tuple<string, string>>()
    {
        new Tuple<string, string>("/auth/register", "POST"),
        new Tuple<string, string>("/auth/login", "POST"),
        new Tuple<string, string>("/channels/%ID%/messages", "POST"),
        new Tuple<string, string>("/guilds", "POST")
    };

    static void Main()
    {
        Console.Title = "OpenCord";
        requestChecker = new RequestChecker();

        if (!System.IO.Directory.Exists("data"))
        {
            System.IO.Directory.CreateDirectory("data");
        }

        if (!System.IO.File.Exists("data\\current_id.txt"))
        {
            System.IO.File.WriteAllText("data\\current_id.txt", "100000000000000000");
        }

        if (System.IO.File.Exists("data\\users.json"))
        {
            registeredUsers = JsonConvert.DeserializeObject<List<RegisteredUser>>(System.IO.File.ReadAllText("data\\users.json"));
        }

        if (System.IO.File.Exists("data\\banned_ips.json"))
        {
            bannedIpAddresses = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText("data\\banned_ips.json"));
        }

        if (!System.IO.Directory.Exists("data\\guilds"))
        {
            System.IO.Directory.CreateDirectory("data\\guilds");
        }
        else
        {
            foreach (string dir in System.IO.Directory.GetDirectories("data\\guilds"))
            {
                DiscordGuild guild = JsonConvert.DeserializeObject<DiscordGuild>(System.IO.File.ReadAllText(dir + "\\data.json"));
                List<GuildTextChannel> textChannels = JsonConvert.DeserializeObject<List<GuildTextChannel>>(System.IO.File.ReadAllText(dir + "\\textChannels.json"));
                List<GuildVoiceChannel> voiceChannels = JsonConvert.DeserializeObject<List<GuildVoiceChannel>>(System.IO.File.ReadAllText(dir + "\\voiceChannels.json"));
                List<GuildCategoryChannel> categoryChannels = JsonConvert.DeserializeObject<List<GuildCategoryChannel>>(System.IO.File.ReadAllText(dir + "\\categoryChannels.json"));
                guild.channels.AddRange(textChannels);
                guild.channels.AddRange(voiceChannels);
                guild.channels.AddRange(categoryChannels);
                guilds.Add(guild);
            }
        }

        if (!System.IO.Directory.Exists("data\\channels"))
        {
            System.IO.Directory.CreateDirectory("data\\channels");
        }

        if (System.IO.File.Exists("data\\locations.json"))
        {
            locations = JsonConvert.DeserializeObject<List<GuildChannelLocation>>(System.IO.File.ReadAllText("data\\locations.json"));
        }

        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

        Thread thread = new Thread(HandleRequests);
        thread.Priority = ThreadPriority.Highest;
        thread.Start();

        server.KeepClean = false;
        server.AuthenticationSchemes = WebSocketSharp.Net.AuthenticationSchemes.Anonymous;
        server.AddWebSocketService<OpenCordWS>("/ws");
        Disable(server.Log);
        server.Start();

        SCHelp.Process();

        while (true)
        {
            SuperCommands.ProcessCommand(Console.ReadLine());
        }
    }

    public static void Disable(Logger logger)
    {
        var field = logger.GetType().GetField("_output", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(logger, new Action<LogData, string>((d, s) => { }));
    }

    public static void HandleRequests()
    {
        LegitHttpServer.HttpServer server = new LegitHttpServer.HttpServer(9987, true);
        server.Start();

        while (true)
        {
            try
            {
                HttpRequest request = server.HandleRequest();
                ProcessRequest(request);
            }
            catch
            {

            }
        }
    }

    public static void ProcessRequest(HttpRequest request)
    {
        try
        {
            HttpResponse response = new HttpResponse();

            string entityBody = "";
            string ip = request.GetIpAddress();
            bool ipBanned = false;

            foreach (string bannedIP in bannedIpAddresses)
            {
                if (bannedIP.Equals(ip))
                {
                    ipBanned = true;
                    break;
                }
            }

            if (ipBanned)
            {
                return;
            }

            string resourceURL = request.GetURI();
            Console.WriteLine(resourceURL);

            if (resourceURL == "/cdn-cgi/bm/cv/669835187/api.js")
            {
                resourceURL = "/assets/api.js";
            }

            if (resourceURL.Equals("/login"))
            {
                response.AddHeader("Content-Type", "text/html");
                Utils.InjectResponse(request, response, System.IO.File.ReadAllBytes("client\\login\\login-normal.html"), 200, "OK");
                return;
            }
            else if (resourceURL.StartsWith("/assets/"))
            {
                string asset = resourceURL.Substring("/assets/".Length);

                if (!System.IO.File.Exists("client\\assets\\" + asset))
                {
                    if (!AUTO_ASSETS_DOWNLOAD)
                    {
                        Utils.InjectResponse(request, response, "{\"message\":\"Resource Not Found\",\"code\":404}", 404, "Not Found");
                        return;
                    }
                    else
                    {
                        byte[] downloaded = Utils.DownloadDiscordAsset(asset);

                        if (downloaded == null)
                        {
                            Utils.InjectResponse(request, response, "{\"message\":\"Resource Not Found\",\"code\":404}", 404, "Not Found");
                            return;
                        }

                        System.IO.File.WriteAllBytes("client\\assets\\" + asset, Utils.DownloadDiscordAsset(asset));
                    }
                }

                if (asset.EndsWith(".js"))
                {
                    response.AddHeader("Content-Type", "application/javascript");
                }
                else if (asset.EndsWith(".ico"))
                {
                    response.AddHeader("Content-Type", "image/vnd.microsoft.icon");
                }
                else if (asset.EndsWith(".png"))
                {
                    response.AddHeader("Content-Type", "image/png");
                }
                else if (asset.EndsWith(".svg"))
                {
                    response.AddHeader("Content-Type", "image/svg+xml");
                }
                else if (asset.EndsWith(".mp3"))
                {
                    response.AddHeader("Content-Type", "audio/mpeg");
                }
                else if (asset.EndsWith(".webm"))
                {
                    response.AddHeader("Content-Type", "video/webm");
                }
                else if (asset.EndsWith(".wasm"))
                {
                    response.AddHeader("Content-Type", "application/wasm");
                }
                else if (asset.EndsWith(".woff"))
                {
                    response.AddHeader("Content-Type", "application/font-woff");
                }

                if (CACHE_DATA)
                {
                    response.AddHeader("Cache-Control", "public, max-age=2592000");
                }

                Utils.InjectResponse(request, response, System.IO.File.ReadAllBytes("client\\assets\\" + asset), 200, "OK");
                return;
            }

            string authorization = "";

            try
            {
                authorization = request.GetHeader("Authorization");
            }
            catch
            {

            }

            bool ipExists = false;
            OpenCordAddress openCordAddress = null;

            foreach (OpenCordAddress address in openCordAddresses)
            {
                if (address.ip.Equals(ip))
                {
                    openCordAddress = address;
                    ipExists = true;
                    break;
                }
            }

            if (!ipExists)
            {
                openCordAddress = new OpenCordAddress();
                openCordAddress.ip = ip;
                openCordAddress.registerRatelimit = -1;
                openCordAddress.loginRatelimit = -1;
                openCordAddresses.Add(openCordAddress);
            }

            if (request.GetMethodStr().Equals("GET") && request.HasBody())
            {
                Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                return;
            }

            if (request.HasBody())
            {
                entityBody = request.GetBody();
            }

            if (request.GetMethodStr() == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Headers", "*");
                response.AddHeader("Access-Control-Allow-Methods", "*");
            }

            response.AddHeader("Access-Control-Allow-Origin", "*");

            if (request.GetMethodStr() == "OPTIONS")
            {
                return;
            }

            response.AddHeader("Content-Type", "application/json");

            if (entityBody != "")
            {
                if (request.GetHeader("Content-Type") != "application/json")
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    return;
                }
            }

            foreach (HttpHeader header in request.GetHeaders())
            {
                if (header.GetName().ToLower().Contains("postman") || header.GetName().ToLower().Contains("x-real-ip"))
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    return;
                }
            }

            foreach (HttpHeader header in request.GetHeaders())
            {
                bool exists = false;

                foreach (string anotherKey in allowedHeaders)
                {
                    if (header.GetName().Equals(anotherKey))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    return;
                }
            }

            if (!Utils.IsLocaleAllowed(request.GetHeader("X-Discord-Locale")))
            {
                Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                return;
            }

            if (!request.GetURI().StartsWith($"/api/v{API_VERSION}"))
            {
                PunishManager.BanIPAddress(ip);
                Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                return;
            }

            string url = request.GetURI().Substring($"/api/v{API_VERSION}".Length, request.GetURI().Length - $"/api/v{API_VERSION}".Length);
            string method = request.GetMethodStr();
            bool isNonAuthed = false;

            foreach (Tuple<string, string> originRequest in originRequests)
            {
                if (Utils.IsURLValid(url, originRequest.Item1).Item1 && method.ToLower().Equals(originRequest.Item2.ToLower()))
                {
                    if (request.GetHeader("Origin") != "http://" + ipAddress + ":" + httpPort)
                    {
                        Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                        return;
                    }
                }
            }

            foreach (Tuple<string, string> nonAuthed in allowedNonAuthedRequests)
            {
                if (Utils.IsURLValid(url, nonAuthed.Item1).Item1 && method.ToLower().Equals(nonAuthed.Item2.ToLower()))
                {
                    isNonAuthed = true;
                    break;
                }
            }

            if (isNonAuthed)
            {
                if (url.Equals("/auth/register") && method.Equals("POST"))
                {
                    RequestRegisterUser.Process(openCordAddress, entityBody, response, ip, request);
                }
                else if (url.Equals("/auth/login") && method.Equals("POST"))
                {
                    RequestLoginUser.Process(openCordAddress, entityBody, response, ip, request);
                }
            }
            else
            {
                bool isAuthed = false;
                string[] urlIDs = new string[] { }, urlNumbers = new string[] { };

                foreach (Tuple<string, string> authed in allowedAuthedRequests)
                {
                    if (Utils.IsURLValid(url, authed.Item1).Item1 && method.ToLower().Equals(authed.Item2.ToLower()))
                    {
                        urlIDs = Utils.IsURLValid(url, authed.Item1).Item2;
                        urlNumbers = Utils.IsURLValid(url, authed.Item1).Item3;
                        isAuthed = true;
                        break;
                    }
                }

                if (isAuthed)
                {
                    if (authorization.Replace(" ", "").Replace('\t'.ToString(), "").Length != 32)
                    {
                        Utils.InjectResponse(request, response, "{}", 401, "Unauthorized");
                        return;
                    }

                    response.AddHeader("Content-Type", "application/json");
                    RegisteredUser user = null;

                    foreach (RegisteredUser registeredUser in registeredUsers)
                    {
                        if (registeredUser.token.Equals(authorization))
                        {
                            user = registeredUser;
                            break;
                        }
                    }

                    if (user == null)
                    {
                        Utils.InjectResponse(request, response, "{}", 401, "Unauthorized");
                        return;
                    }

                    bool disconnect = false;

                    if (user.ip != ip)
                    {
                        user.token = null;
                        disconnect = true;
                        Utils.InjectResponse(request, response, "{}", 401, "Unauthorized");
                        return;
                    }

                    if (user.locked)
                    {
                        Utils.InjectResponse(request, response, "{\"message\":\"You need to verify your account in order to perform this action.\",\"code\":40002}", 403, "Forbidden");
                        return;
                    }

                    WebSocketUser wsUser = null;

                    foreach (WebSocketUser webSocketUser in OpenCordWS.users)
                    {
                        if (webSocketUser.token.Equals(authorization))
                        {
                            wsUser = webSocketUser;
                            break;
                        }
                    }

                    if (wsUser == null)
                    {
                        Utils.InjectResponse(request, response, "{}", 401, "Unauthorized");
                        PunishManager.LockUser(user, wsUser);
                        return;
                    }

                    if (!(request.GetURI().Equals($"/api/v{API_VERSION}/users/@me/settings") && request.GetMethodStr().Equals("PATCH") && entityBody.StartsWith("{\"locale\":")))
                    {
                        if (request.GetHeader("X-Discord-Locale") != user.locale)
                        {
                            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                            PunishManager.BanUser(user, wsUser);
                            return;
                        }
                    }

                    if (disconnect)
                    {
                        wsManager.CloseSession(wsUser.id);
                        Utils.InjectResponse(request, response, "{}", 401, "Unauthorized");
                        return;
                    }

                    ZlibStreamContext zlibContext = wsUser.zlibStreamContext;

                    if (url.Equals("/hypesquad/online") && method.Equals("POST"))
                    {
                        RequestSetHypesquad.Process(openCordAddress, entityBody, response, ip, request, user, wsUser, zlibContext, urlIDs, urlNumbers);
                    }
                    else if (url.Equals("/users/" + user.id + "/profile?with_mutual_guilds=false") && method.Equals("GET"))
                    {
                        RequestGetOwnUser.Process(openCordAddress, entityBody, response, ip, request, user, wsUser, zlibContext, urlIDs, urlNumbers);
                    }
                    else if (url.Equals("/users/@me/settings") && method.Equals("PATCH"))
                    {
                        RequestUpdateSettings.Process(openCordAddress, entityBody, response, ip, request, user, wsUser, zlibContext, urlIDs, urlNumbers);
                    }
                    else if (url.Equals("/guilds") && method.Equals("POST"))
                    {
                        RequestCreateGuild.Process(openCordAddress, entityBody, response, ip, request, user, wsUser, zlibContext, urlIDs, urlNumbers);
                    }
                    else if (Utils.IsURLValid(url, "/channels/%ID%/messages?limit=%NUM%").Item1 && method.Equals("GET"))
                    {
                        RequestGetMessages.Process(openCordAddress, entityBody, response, ip, request, user, wsUser, zlibContext, urlIDs, urlNumbers);
                    }
                    else if (Utils.IsURLValid(url, "/channels/%ID%/messages?before=%ID%&limit=%NUM%").Item1 && method.Equals("GET"))
                    {
                        RequestGetBeforeMessages.Process(openCordAddress, entityBody, response, ip, request, user, wsUser, zlibContext, urlIDs, urlNumbers);
                    }
                    else if (Utils.IsURLValid(url, "/channels/%ID%/messages").Item1 && method.Equals("POST"))
                    {
                        RequestSendMessage.Process(openCordAddress, entityBody, response, ip, request, user, wsUser, zlibContext, urlIDs, urlNumbers);
                    }
                    else if (Utils.IsURLValid(url, "/channels/%ID%/messages/%ID%").Item1 && method.Equals("PATCH"))
                    {
                        RequestEditMessage.Process(openCordAddress, entityBody, response, ip, request, user, wsUser, zlibContext, urlIDs, urlNumbers);
                    }
                }
                else
                {
                    Utils.InjectResponse(request, response, "{\"message\":\"Resource Not Found\",\"code\":404}", 404, "Not Found");
                }
            }
        }
        catch
        {
            Utils.InjectResponse(request, new HttpResponse(), "{}", 400, "Bad Request");
        }
    }
}