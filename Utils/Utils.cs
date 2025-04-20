using System.Security.Cryptography;
using System.Text;
using System;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;
using System.Linq;
using System.Net;
using System.Reflection;
using LegitHttpServer;

public class Utils
{
    public static string[] allowedLocales = new string[] { "da", "de", "en-GB", "en-US", "en-ES", "fr", "hr", "it", "lt", "hu", "nl", "no", "pl", "pt-BR", "ro", "fi", "sv-SE", "vi", "tr", "cs", "el", "bg", "ru", "uk", "hi", "th", "zh-CN", "ja", "zh-TW", "ko" };
    public static ResourceSemaphore idSemaphore = new ResourceSemaphore();
    private static char[] theNumbers = "0123456789".ToCharArray();

    public static IEnumerable<string> SplitToLines(string input)
    {
        if (input == null)
        {
            yield break;
        }

        using (System.IO.StringReader reader = new System.IO.StringReader(input))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }

    public static bool IsEmailValid(string email)
    {
        if (email.Length > 320)
        {
            return false;
        }

        if (!(new EmailAddressAttribute().IsValid(email)))
        {
            return false;
        }

        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith(".") || trimmedEmail.Contains("+"))
        {
            return false;
        }

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            
            if (addr.Address != trimmedEmail)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        string lowerEmail = email.ToLower();

        if (lowerEmail.EndsWith("@gmail.com"))
        {
            string realEmail = Strings.Split(lowerEmail, "@")[0];
            int dots = 0;

            foreach (char c in realEmail)
            {
                if (c.Equals('.')) dots++;
            }

            if (dots >= 4)
            {
                foreach (string pattern in new string[] { "tmp", "t.m.p", "t.mp", "tm.p" })
                {
                    if (realEmail.EndsWith(pattern))
                    {
                        return false;
                    }
                }
            }
        }

        foreach (string blacklistedDomain in SplitToLines(OpenCord.Properties.Resources.domains))
        {
            if (email.EndsWith(blacklistedDomain))
            {
                return false;
            }
        }

        foreach (string blacklistedMail in SplitToLines(OpenCord.Properties.Resources.emails))
        {
            if (lowerEmail.EndsWith("@" + blacklistedMail))
            {
                return false;
            }
        }

        return true;
    }

    public static string GetToken(RegisteredUser registeredUser)
    {
        return CryptoUtils.GetMD5(CryptoUtils.EncryptAES256("{\"id\":\"" + registeredUser.id + "\",\"email\":\"" + registeredUser.email + "\",\"password\":\"" + registeredUser.password + "\",\"date_of_birth\":\"" + registeredUser.date_of_birth + "\",\"timestamp\":\"" + registeredUser.timestamp + "\",\"ip\":\"" + registeredUser.ip + "\",\"deviceUUID\":\"" + registeredUser.deviceUUID + "\"}", "OPEN_CORD_932123"));
    }

    public static string Base64Decode(string base64)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    public static byte[] ReadFully(Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    public static int GetSuperUserFlags()
    {
        return 1 | 2 | 4 | 8 | 16 | 32 | 64 | 128 | 256 | 512 | 1024 | 2048 | 4096 | 16384 | 32768 | 65536 | 131072 | 262144 | 524288;
    }

    public static bool IsHeadersOrderValid(string[] headers, string[] realHeaders)
    {
        if (headers.Length != realHeaders.Length)
        {
            return false;
        }

        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i] != realHeaders[i])
            {
                return false;
            }
        }

        return true;
    }

    public static void InjectResponse(HttpRequest request, HttpResponse response, string content, int statusCode = 200, string statusDescription = "OK")
    {
        response.SetStatusCode(statusCode);
        response.SetStatusDescription(statusDescription);
        response.SetBody(content);
        response.AddHeader("Content-Length", response.GetBodyLength().ToString());
        request.WriteResponse(response);
    }

    public static void InjectResponse(HttpRequest request, HttpResponse response, byte[] content, int statusCode = 200, string statusDescription = "OK")
    {
        response.SetStatusCode(statusCode);
        response.SetStatusDescription(statusDescription);
        response.SetBody(content);
        response.AddHeader("Content-Length", response.GetBodyLength().ToString());
        request.WriteResponse(response);
    }

    public static string GetNewID()
    {
        while (idSemaphore.IsResourceNotAvailable())
        {
            System.Threading.Thread.Sleep(HTTPManager.SEMAPHORE_SLEEP);
        }

        
        if (idSemaphore.IsResourceAvailable())
        {
            idSemaphore.LockResource();
            decimal currentId = decimal.Parse(System.IO.File.ReadAllText("data\\current_id.txt"));
            currentId++;
            System.IO.File.WriteAllText("data\\current_id.txt", currentId.ToString());
            idSemaphore.UnlockResource();
            return currentId.ToString();
        }

        return "";
    }

    public static string GetFriendSourceFlags(UserSettings settings)
    {
        string builtJson = "{";

        if (settings.friend_source_flags.all)
        {
            builtJson += "\"all\":true";
        }

        if (settings.friend_source_flags.mutual_friends)
        {
            if (builtJson != "{")
            {
                builtJson += ",\"mutual_friends\":true";
            }
        }

        if (settings.friend_source_flags.mutual_guilds)
        {
            if (builtJson != "{")
            {
                builtJson += ",\"mutual_guilds\":true";
            }
        }

        return builtJson + "}";
    }

    public static bool IsLocaleAllowed(string locale)
    {
        foreach (string aLocale in allowedLocales)
        {
            if (locale.Equals(aLocale))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetGuildData(DiscordGuild guild)
    {
        string rolesData = Newtonsoft.Json.JsonConvert.SerializeObject(guild.roles);
        string membersData = Newtonsoft.Json.JsonConvert.SerializeObject(guild.firstMember);
        string channelsData = "";

        foreach (var channel in guild.channels)
        {
            string parsed = "";

            if (channel.GetType() == typeof(GuildTextChannel))
            {
                GuildTextChannel textChannel = (GuildTextChannel)channel;
                parsed = Newtonsoft.Json.JsonConvert.SerializeObject(textChannel);
            }
            else if (channel.GetType() == typeof(GuildCategoryChannel))
            {
                GuildCategoryChannel categoryChannel = (GuildCategoryChannel)channel;
                parsed = Newtonsoft.Json.JsonConvert.SerializeObject(categoryChannel);
            }
            else if (channel.GetType() == typeof(GuildVoiceChannel))
            {
                GuildVoiceChannel voiceChannel = (GuildVoiceChannel)channel;
                parsed = Newtonsoft.Json.JsonConvert.SerializeObject(voiceChannel);
            }

            if (channelsData == "")
            {
                channelsData = parsed;
            }
            else
            {
                channelsData += "," + parsed;
            }
        }

        return "{\"owner_id\":\"" + guild.owner_id + "\",\"hub_type\":" + JSONUtils.ParseJsonString(guild.hub_type) + ",\"name\":\"" + guild.name + "\",\"discovery_splash\":" + JSONUtils.ParseJsonString(guild.discovery_splash) + ",\"joined_at\":\"" + guild.joined_at + "\",\"verification_level\":" + guild.verification_level + ",\"description\":" + JSONUtils.ParseJsonString(guild.description) + ",\"features\":[],\"vanity_url_code\":" + JSONUtils.ParseJsonString(guild.vanity_url_code) + ",\"application_id\":" + JSONUtils.ParseJsonString(guild.application_id) + ",\"max_members\":" + guild.max_members + ",\"member_count\":" + guild.member_count + ",\"region\":\"" + guild.region + "\",\"large\":" + guild.large.ToString().ToLower() + ",\"voice_states\":[],\"roles\":" + rolesData + ",\"lazy\":" + guild.lazy.ToString().ToLower() + ",\"guild_scheduled_events\":[],\"preferred_locale\":\"" + guild.preferred_locale + "\",\"afk_channel_id\":" + JSONUtils.ParseJsonString(guild.afk_channel_id) + ",\"premium_progress_bar_enabled\":" + guild.premium_progress_bar_enabled.ToString().ToLower() + ",\"embedded_activities\":[],\"system_channel_flags\":" + guild.system_channel_flags + ",\"icon\":" + JSONUtils.ParseJsonString(guild.icon) + ",\"emojis\":[],\"nsfw_level\":" + guild.nsfw_level + ",\"threads\":[],\"banner\":" + JSONUtils.ParseJsonString(guild.banner) + ",\"default_message_notifications\":" + guild.default_message_notifications + ",\"stickers\":[],\"premium_tier\":" + guild.premium_tier + ",\"channels\":[" + channelsData + "],\"system_channel_id\":" + JSONUtils.ParseJsonString(guild.system_channel_id) + ",\"application_command_count\":" + guild.application_command_count + ",\"splash\":" + JSONUtils.ParseJsonString(guild.splash) + ",\"explicit_content_filter\":" + guild.explicit_content_filter + ",\"application_command_counts\":{\"1\":0,\"2\":0,\"3\":0},\"id\":\"" + guild.id + "\",\"members\":[" + membersData + "],\"presences\":[],\"stage_instances\":[],\"rules_channel_id\":" + JSONUtils.ParseJsonString(guild.rules_channel_id) + ",\"premium_subscription_count\":" + guild.premium_subscription_count + ",\"afk_timeout\":" + guild.afk_timeout + ",\"max_video_channel_users\":" + guild.max_video_channel_users + ",\"mfa_level\":" + guild.mfa_level + ",\"nsfw\":" + guild.nsfw.ToString().ToLower() + ",\"public_updates_channel_id\":" + JSONUtils.ParseJsonString(guild.public_updates_channel_id) + "}";
    }

    public static string ReplaceFirst(string text, string search, string replace)
    {
        int pos = text.IndexOf(search);

        if (pos < 0)
        {
            return text;
        }

        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    public static Tuple<bool, string[], string[]> IsURLValid(string url, string format)
    {
        try
        {
            List<string> ids = new List<string>(), numbers = new List<string>();
            bool isAdding = false;
            string actualNumber = "";

            foreach (char c in url.ToCharArray())
            {
                bool exist = false;

                foreach (char s in theNumbers)
                {
                    if (c.Equals(s))
                    {
                        exist = true;
                        break;
                    }
                }

                if (exist)
                {
                    actualNumber += c.ToString();
                }
                else
                {
                    if (actualNumber != "")
                    {
                        if (actualNumber.Length == 18)
                        {
                            ids.Add(actualNumber);                        
                        }
                        else
                        {
                            numbers.Add(actualNumber);
                        }

                        actualNumber = "";
                    }
                }
            }

            if (actualNumber != "")
            {
                if (actualNumber.Length == 18)
                {
                    ids.Add(actualNumber);
                }
                else
                {
                    numbers.Add(actualNumber);
                }

                actualNumber = "";
            }

            foreach (string num in ids)
            {
                format = ReplaceFirst(format, "%ID%", num);
            }

            foreach (string num in numbers)
            {
                format = ReplaceFirst(format, "%NUM%", num);
            }

            if (url.Equals(format))
            {
                return new Tuple<bool, string[], string[]>(true, ids.ToArray(), numbers.ToArray());
            }

            return new Tuple<bool, string[], string[]>(false, ids.ToArray(), numbers.ToArray());
        }
        catch
        {
            return new Tuple<bool, string[], string[]>(false, null, null);
        }
    }

    public static byte[] DownloadDiscordAsset(string asset)
    {
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://discord.com/assets/" + asset);

            request.Proxy = null;
            request.UseDefaultCredentials = false;
            request.AllowAutoRedirect = false;

            var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            request.Method = "GET";

            var headers = new CustomWebHeaderCollection(new Dictionary<string, string>
            {
                ["Host"] = "discord.com"
            });

            field.SetValue(request, headers);
            WebResponse response = request.GetResponse();
            byte[] downloaded = ReadFully(response.GetResponseStream());

            response.Close();
            response.Dispose();

            return downloaded;
        }
        catch
        {
            return null;
        }
    }
}