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

public class RequestRegisterUser
{
    public static void Process(OpenCordAddress openCordAddress, string entityBody, HttpResponse response, string ip, HttpRequest request)
    {
        try
        {
            if (openCordAddress.registerRatelimit != -1)
            {
                if (TimestampUtils.GetTimestamp() >= openCordAddress.registerRatelimit)
                {
                    openCordAddress.registerRatelimit = -1;
                    openCordAddress.registerRequests = 0;
                }
                else
                {
                    openCordAddress.registerRequests++;
                    long remainingSeconds = (openCordAddress.registerRatelimit - TimestampUtils.GetTimestamp());
                    Utils.InjectResponse(request, response, "{\"message\":\"You are being rate limited.\",\"global\":false,\"retry_after\":" + remainingSeconds + ".000}", 429, "Too Many Requests");

                    if (openCordAddress.registerRequests > 15)
                    {
                        PunishManager.BanIPAddress(ip);
                    }

                    return;
                }
            }
            else
            {
                openCordAddress.registerRatelimit = TimestampUtils.GetTimestamp() + 500;
            }

            if (!request.GetHeader("Referer").StartsWith("http://" + HTTPManager.ipAddress + ":" + HTTPManager.httpPort + "/register"))
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"email\":{\"_errors\":[{\"code\":\"EMAIL_INVALID\",\"message\":\"Invalid registration form.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                PunishManager.BanIPAddress(ip);
                return;
            }

            dynamic jss = JObject.Parse(entityBody);
            JObject jsonObj = JObject.Parse(entityBody);
            Dictionary<string, object> dictObj = jsonObj.ToObject<Dictionary<string, object>>();
            int currentKey = 0;

            if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "email", "username", "password", "invite", "consent", "date_of_birth", "gift_code_sku_id", "captcha_key" }) || jss.invite != null || jss.consent != true || jss.gift_code_sku_id != null || jss.captcha_key != null)
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"email\":{\"_errors\":[{\"code\":\"EMAIL_INVALID\",\"message\":\"Invalid registration form.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                PunishManager.BanIPAddress(ip);
                return;
            }

            int registeredAccounts = 0;

            foreach (RegisteredUser user in HTTPManager.registeredUsers)
            {
                if (user.ip.Equals(ip))
                {
                    registeredAccounts++;

                    if (registeredAccounts >= HTTPManager.MAX_ACCOUNTS_PER_IP)
                    {
                        break;
                    }
                }
            }

            if (registeredAccounts >= HTTPManager.MAX_ACCOUNTS_PER_IP)
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"email\":{\"_errors\":[{\"code\":\"EMAIL_INVALID\",\"message\":\"You can not register more than " + HTTPManager.MAX_ACCOUNTS_PER_IP + " accounts.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                return;
            }

            if (!Utils.IsEmailValid((string)jss.email))
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"email\":{\"_errors\":[{\"code\":\"EMAIL_INVALID\",\"message\":\"Invalid e-mail address.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                return;
            }

            if (((string)jss.password).Length < 6 || ((string)jss.password).Length > 50)
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"password\":{\"_errors\":[{\"code\":\"BASE_TYPE_MIN_LENGTH\",\"message\":\"Bad password complexity. Minimum length is of 6 characters.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                return;
            }

            if (((string)jss.username).Length > 100)
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"username\":{\"_errors\":[{\"code\":\"USERNAME_TOO_MANY_USERS\",\"message\":\"Maximum length of the username is of 100 characters.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                return;
            }

            if (((string)jss.username).Length == 0)
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"username\":{\"_errors\":[{\"code\":\"USERNAME_TOO_MANY_USERS\",\"message\":\"Invalid username.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                return;
            }

            bool alreadyRegistered = false;

            foreach (RegisteredUser user in HTTPManager.registeredUsers)
            {
                if (user.email.Equals((string)jss.email))
                {
                    alreadyRegistered = true;
                    break;
                }
            }

            if (alreadyRegistered)
            {
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"email\":{\"_errors\":[{\"code\":\"EMAIL_ALREADY_REGISTERED\",\"message\":\"This e-mail address is already registered.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                return;
            }

            string date_of_birth = jss.date_of_birth;
            bool isDateValid = true;

            if (date_of_birth.Length != 10)
            {
                isDateValid = false;
            }
            else
            {
                int symbols = 0;

                foreach (char c in date_of_birth.ToCharArray())
                {
                    if (c == '-')
                    {
                        symbols++;
                    }
                }

                if (symbols >= 3)
                {
                    isDateValid = false;
                }
                else
                {
                    if (!TimestampUtils.IsDateValid(date_of_birth))
                    {
                        isDateValid = false;
                    }
                    else
                    {
                        int age = TimestampUtils.GetAge(TimestampUtils.ParseDate(date_of_birth), DateTime.Now);

                        if (age < HTTPManager.MIN_AGE)
                        {
                            isDateValid = false;
                        }
                    }
                }
            }

            if (!isDateValid)
            {
                Console.WriteLine("8");
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"email\":{\"_errors\":[{\"code\":\"EMAIL_ALREADY_REGISTERED\",\"message\":\"Invalid date of birth.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                return;
            }

            response.SetStatusCode((int)HttpStatusCode.Created);
            response.SetStatusDescription("Created");

            RegisteredUser registeredUser = new RegisteredUser();

            registeredUser.email = jss.email;
            registeredUser.username = jss.username;
            registeredUser.password = CryptoUtils.GetMD5((string)jss.password);
            registeredUser.date_of_birth = date_of_birth;
            registeredUser.accent_color = -1;
            registeredUser.avatar = null;
            registeredUser.banner = null;
            registeredUser.banner_color = -1;
            registeredUser.bio = "";
            registeredUser.flags = 0;
            registeredUser.verified = true;
            registeredUser.purchased_flags = 0;
            registeredUser.public_flags = 0;
            registeredUser.premium_type = 0;
            registeredUser.phone = null;
            registeredUser.nsfw_allowed = false;
            registeredUser.mfa_enabled = false;
            registeredUser.locale = request.GetHeader("X-Discord-Locale");
            registeredUser.ip = ip;
            registeredUser.premium = false;
            registeredUser.settings = new UserSettings();
            registeredUser.settings.friend_source_flags = new FriendSourceFlags();
            registeredUser.userFlags = new UserFlags();
            registeredUser.superProperties = request.GetHeader("X-Super-Properties");
            registeredUser.id = Utils.GetNewID();
            registeredUser.deviceUUID = "sapessi";

            string discriminator = "";
            int discriminatorValue = 0;
            bool isSet = false;

            while (true)
            {
                discriminatorValue++;

                if (discriminatorValue.ToString().Length == 1)
                {
                    discriminator = "000" + discriminatorValue.ToString();
                }
                else if (discriminatorValue.ToString().Length == 2)
                {
                    discriminator = "00" + discriminatorValue.ToString();
                }
                else if (discriminatorValue.ToString().Length == 3)
                {
                    discriminator = "0" + discriminatorValue.ToString();
                }
                else
                {
                    discriminator = discriminatorValue.ToString();
                }

                bool exists = false;

                foreach (RegisteredUser user in HTTPManager.registeredUsers)
                {
                    if (user.username.Equals(registeredUser.username) && user.discriminator.Equals(discriminator))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    registeredUser.discriminator = discriminator;
                    isSet = true;
                    break;
                }
            }

            if (!isSet)
            {
                Console.WriteLine("9");
                Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"username\":{\"_errors\":[{\"code\":\"USERNAME_TOO_MANY_USERS\",\"message\":\"Too many users have this username, please try another.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
                return;
            }

            registeredUser.timestamp = TimestampUtils.GetTimestamp();
            registeredUser.token = Utils.GetToken(registeredUser);
            HTTPManager.registeredUsers.Add(registeredUser);
            DataManager.SaveUsers();
            Utils.InjectResponse(request, response, "{\"token\":\"" + registeredUser.token + "\"}", 201, "Created");
        }
        catch
        {
            Console.WriteLine("10");
            Utils.InjectResponse(request, response, "{\"code\":50035,\"errors\":{\"email\":{\"_errors\":[{\"code\":\"EMAIL_INVALID\",\"message\":\"Invalid registration form.\"}]}},\"message\":\"Invalid Form Body\"}", 400, "Bad Request");
            PunishManager.BanIPAddress(ip);
        }
    }
}