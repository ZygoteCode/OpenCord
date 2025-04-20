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

public class RequestLoginUser
{
    public static void Process(OpenCordAddress openCordAddress, string entityBody, HttpResponse response, string ip, HttpRequest request)
    {
        try
        {
            response.AddHeader("Content-Type", "application/json");

            if (openCordAddress.loginRatelimit != -1)
            {
                if (TimestampUtils.GetTimestamp() >= openCordAddress.loginRatelimit)
                {
                    openCordAddress.loginRatelimit = -1;
                    openCordAddress.loginRequests = 0;
                }
                else
                {
                    openCordAddress.loginRequests++;
                    long remainingSeconds = (openCordAddress.loginRatelimit - TimestampUtils.GetTimestamp());
                    Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"login\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"You are being rate limited.\"}]},\"password\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"You are being rate limited.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");

                    if (openCordAddress.loginRequests > 15)
                    {
                        PunishManager.BanIPAddress(ip);
                    }

                    return;
                }
            }
            else
            {
                openCordAddress.loginRatelimit = TimestampUtils.GetTimestamp() + 15;
            }

            dynamic jss = JObject.Parse(entityBody);
            bool isValid = true;

            if (request.GetHeader("Referer") != "http://" + HTTPManager.ipAddress + ":" + HTTPManager.httpPort + "/login")
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"login\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Invalid username or password.\"}]},\"password\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Invalid username or password.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                PunishManager.BanIPAddress(ip);
                return;
            }

            if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "login", "password", "undelete", "captcha_key", "login_source", "gift_code_sku_id" }) || jss.undelete != false || jss.captcha_key != null || jss.login_source != null || jss.gift_code_sku_id != null)
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"login\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Invalid username or password.\"}]},\"password\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Invalid username or password.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                PunishManager.BanIPAddress(ip);
                return;
            }

            string login = (string)jss.login, password = CryptoUtils.GetMD5((string)jss.password);

            RegisteredUser registeredUser = null;

            foreach (RegisteredUser user in HTTPManager.registeredUsers)
            {
                string username = user.username + "#" + user.discriminator, email = user.email, itsPassword = user.password;

                if (username == login || email == login)
                {
                    if (password == itsPassword)
                    {
                        registeredUser = user;
                        break;
                    }
                }
            }

            if (registeredUser == null)
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"login\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Invalid username or password.\"}]},\"password\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Invalid username or password.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
            }
            else
            {
                if (registeredUser.disabled)
                {
                    Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"login\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Your account is disabled.\"}]},\"password\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Your account is disabled.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                    PunishManager.BanIPAddress(ip);
                }
                else
                {
                    registeredUser.superProperties = request.GetHeader("X-Super-Properties");
                    registeredUser.ip = ip;
                    registeredUser.deviceUUID = "sapessi";
                    registeredUser.token = Utils.GetToken(registeredUser);
                    DataManager.SaveUsers();
                    Utils.InjectResponse(request, response, "{\"token\":\"" + registeredUser.token + "\"}", 200, "OK");
                }
            }
        }
        catch
        {
            Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"login\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Invalid username or password.\"}]},\"password\":{\"_errors\":[{\"code\":\"INVALID_LOGIN\",\"message\":\"Invalid username or password.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
            PunishManager.BanIPAddress(ip);
        }
    }
}