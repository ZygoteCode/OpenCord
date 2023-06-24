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

public class RequestUpdateSettings
{
    public static void Process(OpenCordAddress openCordAddress, string entityBody, HttpResponse response, string ip, HttpRequest request, RegisteredUser user, WebSocketUser wsUser, ZlibStreamContext zlibContext, string[] IDs, string[] numbers)
    {
        try
        {
            if (openCordAddress.updateSettingsTime == 0)
            {
                openCordAddress.updateSettingsTime = TimestampUtils.GetTimestamp();
            }
            else
            {
                if (TimestampUtils.GetTimestamp() >= openCordAddress.updateSettingsTime + 300)
                {
                    openCordAddress.updateSettingsTime = TimestampUtils.GetTimestamp();
                    openCordAddress.updateSettingsRequests = 0;
                }
            }

            if (openCordAddress.updateSettingsRateLimit != -1)
            {
                if (TimestampUtils.GetTimestamp() >= openCordAddress.updateSettingsRateLimit)
                {
                    openCordAddress.updateSettingsRateLimit = -1;
                    openCordAddress.updateSettingsRequests = 1;
                }
                else
                {
                    openCordAddress.updateSettingsRequests++;
                    long remainingSeconds = (openCordAddress.updateSettingsRateLimit - TimestampUtils.GetTimestamp());
                    Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"You are being rate limited.\",\"retry_after\":" + remainingSeconds + "}", 429, "Too Many Requests");

                    if (openCordAddress.updateSettingsRequests > 9)
                    {
                        PunishManager.BanIPAddress(ip);
                    }

                    return;
                }
            }
            else
            {
                openCordAddress.updateSettingsRateLimit = -1;
                openCordAddress.updateSettingsRequests++;

                if (openCordAddress.updateSettingsRequests > 7)
                {
                    openCordAddress.updateSettingsRateLimit = TimestampUtils.GetTimestamp() + 300;
                    Utils.InjectResponse(request, response, "{\"code\":20028,\"global\":false,\"message\":\"You are being rate limited.\",\"retry_after\":10}", 429, "Too Many Requests");
                    return;
                }
            }

            if (entityBody.Equals("{\"theme\":\"dark\"}"))
            {
                if (user.settings.theme == "light")
                {
                    user.settings.theme = "dark";
                    DataManager.SaveUsers();
                }

                string builtJson = "{\"locale\":\"" + user.locale + "\",\"show_current_game\":" + user.settings.show_current_game.ToString().ToLower() + ",\"restricted_guilds\":[],\"default_guilds_restricted\":" + user.settings.default_guilds_restricted.ToString().ToLower() + ",\"inline_attachment_media\":" + user.settings.inline_attachment_media.ToString().ToLower() + ",\"inline_embed_media\":" + user.settings.inline_embed_media.ToString().ToLower() + ",\"gif_auto_play\":" + user.settings.gif_auto_play.ToString().ToLower() + ",\"render_embeds\":" + user.settings.render_embeds.ToString().ToLower() + ",\"render_reactions\":" + user.settings.render_reactions.ToString().ToLower() + ",\"animate_emoji\":" + user.settings.animate_emoji.ToString().ToLower() + ",\"enable_tts_command\":" + user.settings.enable_tts_command.ToString().ToLower() + ",\"message_display_compact\":" + user.settings.message_display_compact.ToString().ToLower() + ",\"convert_emoticons\":" + user.settings.convert_emoticons.ToString().ToLower() + ",\"explicit_content_filter\":" + user.settings.explicit_content_filter + ",\"disable_games_tab\":" + user.settings.disable_games_tab.ToString().ToLower() + ",\"theme\":\"" + user.settings.theme + "\",\"developer_mode\":false,\"guild_positions\":[],\"detect_platform_accounts\":" + user.settings.detect_platform_accounts.ToString().ToLower() + ",\"status\":\"" + user.settings.status + "\",\"afk_timeout\":" + user.settings.afk_timeout + ",\"timezone_offset\":" + user.settings.timezone_offset + ",\"stream_notifications_enabled\":" + user.settings.stream_notifications_enabled.ToString().ToLower() + ",\"allow_accessibility_detection\":" + user.settings.allow_accessibility_detection.ToString().ToLower() + ",\"contact_sync_enabled\":" + user.settings.contact_sync_enabled.ToString().ToLower() + ",\"native_phone_integration_enabled\":" + user.settings.native_phone_integration_enabled.ToString().ToLower() + ",\"animate_stickers\":" + user.settings.animate_stickers + ",\"friend_discovery_flags\":" + user.settings.friend_discovery_flags + ",\"view_nsfw_guilds\":" + user.settings.view_nsfw_guilds.ToString().ToLower() + ",\"passwordless\":" + user.settings.passwordless.ToString().ToLower() + ",\"friend_source_flags\":" + Utils.GetFriendSourceFlags(user.settings) + ",\"guild_folders\":[],\"custom_status\":null}";
                Utils.InjectResponse(request, response, builtJson, 200, "OK");
            }
            else if (entityBody.Equals("{\"theme\":\"light\"}"))
            {
                if (user.settings.theme == "dark")
                {
                    user.settings.theme = "light";
                    DataManager.SaveUsers();
                }

                string builtJson = "{\"locale\":\"" + user.locale + "\",\"show_current_game\":" + user.settings.show_current_game.ToString().ToLower() + ",\"restricted_guilds\":[],\"default_guilds_restricted\":" + user.settings.default_guilds_restricted.ToString().ToLower() + ",\"inline_attachment_media\":" + user.settings.inline_attachment_media.ToString().ToLower() + ",\"inline_embed_media\":" + user.settings.inline_embed_media.ToString().ToLower() + ",\"gif_auto_play\":" + user.settings.gif_auto_play.ToString().ToLower() + ",\"render_embeds\":" + user.settings.render_embeds.ToString().ToLower() + ",\"render_reactions\":" + user.settings.render_reactions.ToString().ToLower() + ",\"animate_emoji\":" + user.settings.animate_emoji.ToString().ToLower() + ",\"enable_tts_command\":" + user.settings.enable_tts_command.ToString().ToLower() + ",\"message_display_compact\":" + user.settings.message_display_compact.ToString().ToLower() + ",\"convert_emoticons\":" + user.settings.convert_emoticons.ToString().ToLower() + ",\"explicit_content_filter\":" + user.settings.explicit_content_filter + ",\"disable_games_tab\":" + user.settings.disable_games_tab.ToString().ToLower() + ",\"theme\":\"" + user.settings.theme + "\",\"developer_mode\":false,\"guild_positions\":[],\"detect_platform_accounts\":" + user.settings.detect_platform_accounts.ToString().ToLower() + ",\"status\":\"" + user.settings.status + "\",\"afk_timeout\":" + user.settings.afk_timeout + ",\"timezone_offset\":" + user.settings.timezone_offset + ",\"stream_notifications_enabled\":" + user.settings.stream_notifications_enabled.ToString().ToLower() + ",\"allow_accessibility_detection\":" + user.settings.allow_accessibility_detection.ToString().ToLower() + ",\"contact_sync_enabled\":" + user.settings.contact_sync_enabled.ToString().ToLower() + ",\"native_phone_integration_enabled\":" + user.settings.native_phone_integration_enabled.ToString().ToLower() + ",\"animate_stickers\":" + user.settings.animate_stickers + ",\"friend_discovery_flags\":" + user.settings.friend_discovery_flags + ",\"view_nsfw_guilds\":" + user.settings.view_nsfw_guilds.ToString().ToLower() + ",\"passwordless\":" + user.settings.passwordless.ToString().ToLower() + ",\"friend_source_flags\":" + Utils.GetFriendSourceFlags(user.settings) + ",\"guild_folders\":[],\"custom_status\":null}";
                Utils.InjectResponse(request, response, builtJson, 200, "OK");
            }
            else if (entityBody.StartsWith("{\"friend_source_flags\":{\"all\":"))
            {
                if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "friend_source_flags" }))
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    PunishManager.BanUser(user, wsUser);
                    return;
                }

                if (!JSONUtils.IsJsonOrderValid(entityBody, "friend_source_flags", new string[] { "all", "mutual_friends", "mutual_guilds" }))
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    PunishManager.BanUser(user, wsUser);
                    return;
                }

                dynamic jss = JObject.Parse(entityBody);

                user.settings.friend_source_flags.all = jss.friend_source_flags.all;
                user.settings.friend_source_flags.mutual_friends = jss.friend_source_flags.mutual_friends;
                user.settings.friend_source_flags.mutual_guilds = jss.friend_source_flags.mutual_guilds;

                string builtJson = "{\"locale\":\"" + user.locale + "\",\"show_current_game\":" + user.settings.show_current_game.ToString().ToLower() + ",\"restricted_guilds\":[],\"default_guilds_restricted\":" + user.settings.default_guilds_restricted.ToString().ToLower() + ",\"inline_attachment_media\":" + user.settings.inline_attachment_media.ToString().ToLower() + ",\"inline_embed_media\":" + user.settings.inline_embed_media.ToString().ToLower() + ",\"gif_auto_play\":" + user.settings.gif_auto_play.ToString().ToLower() + ",\"render_embeds\":" + user.settings.render_embeds.ToString().ToLower() + ",\"render_reactions\":" + user.settings.render_reactions.ToString().ToLower() + ",\"animate_emoji\":" + user.settings.animate_emoji.ToString().ToLower() + ",\"enable_tts_command\":" + user.settings.enable_tts_command.ToString().ToLower() + ",\"message_display_compact\":" + user.settings.message_display_compact.ToString().ToLower() + ",\"convert_emoticons\":" + user.settings.convert_emoticons.ToString().ToLower() + ",\"explicit_content_filter\":" + user.settings.explicit_content_filter + ",\"disable_games_tab\":" + user.settings.disable_games_tab.ToString().ToLower() + ",\"theme\":\"" + user.settings.theme + "\",\"developer_mode\":false,\"guild_positions\":[],\"detect_platform_accounts\":" + user.settings.detect_platform_accounts.ToString().ToLower() + ",\"status\":\"" + user.settings.status + "\",\"afk_timeout\":" + user.settings.afk_timeout + ",\"timezone_offset\":" + user.settings.timezone_offset + ",\"stream_notifications_enabled\":" + user.settings.stream_notifications_enabled.ToString().ToLower() + ",\"allow_accessibility_detection\":" + user.settings.allow_accessibility_detection.ToString().ToLower() + ",\"contact_sync_enabled\":" + user.settings.contact_sync_enabled.ToString().ToLower() + ",\"native_phone_integration_enabled\":" + user.settings.native_phone_integration_enabled.ToString().ToLower() + ",\"animate_stickers\":" + user.settings.animate_stickers + ",\"friend_discovery_flags\":" + user.settings.friend_discovery_flags + ",\"view_nsfw_guilds\":" + user.settings.view_nsfw_guilds.ToString().ToLower() + ",\"passwordless\":" + user.settings.passwordless.ToString().ToLower() + ",\"friend_source_flags\":" + Utils.GetFriendSourceFlags(user.settings) + ",\"guild_folders\":[],\"custom_status\":null}";
                Utils.InjectResponse(request, response, builtJson, 200, "OK");
            }
            else if (entityBody.StartsWith("{\"locale\":"))
            {
                if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "locale" }))
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    PunishManager.BanUser(user, wsUser);
                    return;
                }

                dynamic jss = JObject.Parse(entityBody);
                string locale = jss.locale;

                if (!Utils.IsLocaleAllowed(locale))
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    PunishManager.BanUser(user, wsUser);
                    return;
                }

                if (request.GetHeader("X-Discord-Locale") != locale)
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    PunishManager.BanUser(user, wsUser);
                    return;
                }

                user.locale = locale;
                DataManager.SaveUsers();

                string builtJson = "{\"locale\":\"" + user.locale + "\",\"show_current_game\":" + user.settings.show_current_game.ToString().ToLower() + ",\"restricted_guilds\":[],\"default_guilds_restricted\":" + user.settings.default_guilds_restricted.ToString().ToLower() + ",\"inline_attachment_media\":" + user.settings.inline_attachment_media.ToString().ToLower() + ",\"inline_embed_media\":" + user.settings.inline_embed_media.ToString().ToLower() + ",\"gif_auto_play\":" + user.settings.gif_auto_play.ToString().ToLower() + ",\"render_embeds\":" + user.settings.render_embeds.ToString().ToLower() + ",\"render_reactions\":" + user.settings.render_reactions.ToString().ToLower() + ",\"animate_emoji\":" + user.settings.animate_emoji.ToString().ToLower() + ",\"enable_tts_command\":" + user.settings.enable_tts_command.ToString().ToLower() + ",\"message_display_compact\":" + user.settings.message_display_compact.ToString().ToLower() + ",\"convert_emoticons\":" + user.settings.convert_emoticons.ToString().ToLower() + ",\"explicit_content_filter\":" + user.settings.explicit_content_filter + ",\"disable_games_tab\":" + user.settings.disable_games_tab.ToString().ToLower() + ",\"theme\":\"" + user.settings.theme + "\",\"developer_mode\":false,\"guild_positions\":[],\"detect_platform_accounts\":" + user.settings.detect_platform_accounts.ToString().ToLower() + ",\"status\":\"" + user.settings.status + "\",\"afk_timeout\":" + user.settings.afk_timeout + ",\"timezone_offset\":" + user.settings.timezone_offset + ",\"stream_notifications_enabled\":" + user.settings.stream_notifications_enabled.ToString().ToLower() + ",\"allow_accessibility_detection\":" + user.settings.allow_accessibility_detection.ToString().ToLower() + ",\"contact_sync_enabled\":" + user.settings.contact_sync_enabled.ToString().ToLower() + ",\"native_phone_integration_enabled\":" + user.settings.native_phone_integration_enabled.ToString().ToLower() + ",\"animate_stickers\":" + user.settings.animate_stickers + ",\"friend_discovery_flags\":" + user.settings.friend_discovery_flags + ",\"view_nsfw_guilds\":" + user.settings.view_nsfw_guilds.ToString().ToLower() + ",\"passwordless\":" + user.settings.passwordless.ToString().ToLower() + ",\"friend_source_flags\":" + Utils.GetFriendSourceFlags(user.settings) + ",\"guild_folders\":[],\"custom_status\":null}";
                Utils.InjectResponse(request, response, builtJson, 200, "OK");
            }
            else if (entityBody.StartsWith("{\"status\":\""))
            {
                if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "status" }))
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    PunishManager.BanUser(user, wsUser);
                    return;
                }

                dynamic jss = JObject.Parse(entityBody);
                string status = jss.status;

                if (status == "online" || status == "dnd" || status == "idle" || status == "invisible")
                {
                    user.settings.status = status;
                    string builtJson = "{\"locale\":\"" + user.locale + "\",\"show_current_game\":" + user.settings.show_current_game.ToString().ToLower() + ",\"restricted_guilds\":[],\"default_guilds_restricted\":" + user.settings.default_guilds_restricted.ToString().ToLower() + ",\"inline_attachment_media\":" + user.settings.inline_attachment_media.ToString().ToLower() + ",\"inline_embed_media\":" + user.settings.inline_embed_media.ToString().ToLower() + ",\"gif_auto_play\":" + user.settings.gif_auto_play.ToString().ToLower() + ",\"render_embeds\":" + user.settings.render_embeds.ToString().ToLower() + ",\"render_reactions\":" + user.settings.render_reactions.ToString().ToLower() + ",\"animate_emoji\":" + user.settings.animate_emoji.ToString().ToLower() + ",\"enable_tts_command\":" + user.settings.enable_tts_command.ToString().ToLower() + ",\"message_display_compact\":" + user.settings.message_display_compact.ToString().ToLower() + ",\"convert_emoticons\":" + user.settings.convert_emoticons.ToString().ToLower() + ",\"explicit_content_filter\":" + user.settings.explicit_content_filter + ",\"disable_games_tab\":" + user.settings.disable_games_tab.ToString().ToLower() + ",\"theme\":\"" + user.settings.theme + "\",\"developer_mode\":false,\"guild_positions\":[],\"detect_platform_accounts\":" + user.settings.detect_platform_accounts.ToString().ToLower() + ",\"status\":\"" + user.settings.status + "\",\"afk_timeout\":" + user.settings.afk_timeout + ",\"timezone_offset\":" + user.settings.timezone_offset + ",\"stream_notifications_enabled\":" + user.settings.stream_notifications_enabled.ToString().ToLower() + ",\"allow_accessibility_detection\":" + user.settings.allow_accessibility_detection.ToString().ToLower() + ",\"contact_sync_enabled\":" + user.settings.contact_sync_enabled.ToString().ToLower() + ",\"native_phone_integration_enabled\":" + user.settings.native_phone_integration_enabled.ToString().ToLower() + ",\"animate_stickers\":" + user.settings.animate_stickers + ",\"friend_discovery_flags\":" + user.settings.friend_discovery_flags + ",\"view_nsfw_guilds\":" + user.settings.view_nsfw_guilds.ToString().ToLower() + ",\"passwordless\":" + user.settings.passwordless.ToString().ToLower() + ",\"friend_source_flags\":" + Utils.GetFriendSourceFlags(user.settings) + ",\"guild_folders\":[],\"custom_status\":null}";
                    Utils.InjectResponse(request, response, builtJson, 200, "OK");
                }
                else
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    PunishManager.BanUser(user, wsUser);
                }
            }
            else if (entityBody.StartsWith("{\"timezone_offset\":"))
            {
                if (!JSONUtils.IsJsonOrderValid(entityBody, new string[] { "timezone_offset" }))
                {
                    Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                    PunishManager.BanUser(user, wsUser);
                    return;
                }

                Utils.InjectResponse(request, response, "", 200, "OK");
            }
            else
            {
                Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
                PunishManager.BanUser(user, wsUser);
            }
        }
        catch
        {
            Utils.InjectResponse(request, response, "{}", 400, "Bad Request");
            PunishManager.LockUser(user, wsUser);
        }
    }
}