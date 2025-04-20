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

public class WSOP2
{
    public static void Process(WebSocketUser wsUser, string ID, dynamic jss, WebSocketSessionManager Sessions, string data, ZlibStreamContext context)
    {
        if (wsUser.logged)
        {
            OpenCordWS.RemoveUser(ID);
            return;
        }

        string token = jss.d.token;

        if (token.Length != 32)
        {
            return;
        }

        RegisteredUser user = null;

        foreach (RegisteredUser registeredUser in HTTPManager.registeredUsers)
        {
            if (registeredUser.token.Equals(token))
            {
                user = registeredUser;
                break;
            }
        }

        OpenCordToken openCordToken = new OpenCordToken(token, user);

        if (!openCordToken.IsValid())
        {
            OpenCordWS.RemoveUser(ID);
            return;
        }

        wsUser.user = user;

        if (!JSONUtils.IsJsonOrderValid(data, new string[] { "op", "d" }) || !JSONUtils.IsJsonOrderValid(data, "d", new string[] { "token", "capabilities", "properties", "presence", "compress", "client_state" }) || jss.d.capabilities != 253 || jss.d.compress != false || !JSONUtils.IsJsonOrderValid(JObject.Parse(data).GetValue("d")["properties"].ToObject<Dictionary<string, object>>(), new string[] { "os", "browser", "device", "system_locale", "browser_user_agent", "browser_version", "os_version", "referrer", "referring_domain", "referrer_current", "referring_domain_current", "release_channel", "client_build_number", "client_event_source" }) || !JSONUtils.IsJsonOrderValid(JObject.Parse(data).GetValue("d")["presence"].ToObject<Dictionary<string, object>>(), new string[] { "status", "since", "activities", "afk" }) || !JSONUtils.IsJsonOrderValid(JObject.Parse(data).GetValue("d")["client_state"].ToObject<Dictionary<string, object>>(), new string[] { "guild_hashes", "highest_last_message_id", "read_state_version", "user_guild_settings_version", "user_settings_version" }))
        {
            if (wsUser != null)
            {
                if (wsUser.user != null)
                {
                    PunishManager.LockUser(wsUser.user, wsUser);
                }
            }

            return;
        }

        wsUser.logged = true;
        wsUser.token = token;
        user.superProperties = "";
        DataManager.SaveUsers();

        string guildsData = "";

        foreach (DiscordGuild guild in HTTPManager.guilds)
        {
            foreach (string id in guild.guildMembers)
            {
                if (id.Equals(user.id))
                {
                    if (guildsData == "")
                    {
                        guildsData += Utils.GetGuildData(guild);
                    }
                    else
                    {
                        guildsData += "," + Utils.GetGuildData(guild);
                    }

                    break;
                }
            }
        }

        Sessions.SendTo(context.Deflate(Encoding.UTF8.GetBytes("{\"t\":null,\"s\":" + wsUser.sequence + ",\"op\":10,\"d\":{\"heartbeat_interval\":41250,\"_trace\":[\"OPEN_CORD\"]}}")), ID);
        wsUser.sequence++;
        Sessions.SendTo(context.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"READY\",\"s\":" + wsUser.sequence + ",\"op\":0,\"d\":{\"v\":9,\"users\":[],\"user_settings_proto\":null,\"user_settings\":{\"inline_attachment_media\":" + user.settings.inline_attachment_media.ToString().ToLower() + ",\"show_current_game\":" + user.settings.show_current_game.ToString().ToLower() + ",\"friend_source_flags\":" + Utils.GetFriendSourceFlags(user.settings) + ",\"view_nsfw_guilds\":" + user.settings.view_nsfw_guilds.ToString().ToLower() + ",\"enable_tts_command\":" + user.settings.enable_tts_command.ToString().ToLower() + ",\"render_reactions\":" + user.settings.render_reactions.ToString().ToLower() + ",\"gif_auto_play\":" + user.settings.gif_auto_play.ToString().ToLower() + ",\"stream_notifications_enabled\":" + user.settings.stream_notifications_enabled.ToString().ToLower() + ",\"animate_emoji\":" + user.settings.animate_emoji.ToString().ToLower() + ",\"afk_timeout\":" + user.settings.afk_timeout + ",\"detect_platform_accounts\":" + user.settings.detect_platform_accounts.ToString().ToLower() + ",\"status\":\"" + user.settings.status + "\",\"explicit_content_filter\":" + user.settings.explicit_content_filter + ",\"custom_status\":null,\"default_guilds_restricted\":" + user.settings.default_guilds_restricted.ToString().ToLower() + ",\"theme\":\"" + user.settings.theme + "\",\"allow_accessibility_detection\":" + user.settings.allow_accessibility_detection.ToString().ToLower() + ",\"locale\":\"" + user.locale + "\",\"native_phone_integration_enabled\":" + user.settings.native_phone_integration_enabled.ToString().ToLower() + ",\"guild_positions\":[],\"timezone_offset\":" + user.settings.timezone_offset + ",\"friend_discovery_flags\":" + user.settings.friend_discovery_flags + ",\"contact_sync_enabled\":" + user.settings.contact_sync_enabled.ToString().ToLower() + ",\"disable_games_tab\":" + user.settings.disable_games_tab.ToString().ToLower() + ",\"guild_folders\":[],\"inline_embed_media\":" + user.settings.inline_embed_media.ToString().ToLower() + ",\"developer_mode\":" + user.settings.developer_mode.ToString().ToLower() + ",\"render_embeds\":" + user.settings.render_embeds.ToString().ToLower() + ",\"animate_stickers\":" + user.settings.animate_stickers + ",\"message_display_compact\":" + user.settings.message_display_compact.ToString().ToLower() + ",\"convert_emoticons\":" + user.settings.convert_emoticons.ToString().ToLower() + ",\"passwordless\":" + user.settings.passwordless.ToString().ToLower() + ",\"restricted_guilds\":[]},\"user_guild_settings\":{\"version\":0,\"partial\":false,\"entries\":[]},\"user\":{\"verified\":" + user.verified.ToString().ToLower() + ",\"username\":\"" + user.username + "\",\"purchased_flags\":" + user.purchased_flags + ",\"public_flags\":" + user.purchased_flags + ",\"premium\":" + user.premium.ToString().ToLower() + ",\"phone\":" + (user.phone == null ? "null" : "\"" + user.phone + "\"") + ",\"nsfw_allowed\":" + user.nsfw_allowed.ToString().ToLower() + ",\"mobile\":false,\"mfa_enabled\":" + user.mfa_enabled.ToString().ToLower() + ",\"id\":\"" + user.id + "\",\"flags\":" + user.flags + ",\"email\":\"" + user.email + "\",\"discriminator\":\"" + user.discriminator + "\",\"desktop\":false,\"bio\":\"" + user.bio + "\",\"banner_color\":" + (user.banner_color == -1 ? "null" : "\"" + user.banner_color + "\"") + ",\"banner\":" + (user.banner == null ? "null" : "\"" + user.banner + "\"") + ",\"avatar\":" + (user.avatar == null ? "null" : "\"" + user.avatar + "\"") + ",\"accent_color\":" + (user.accent_color == -1 ? "null" : user.accent_color.ToString()) + "},\"tutorial\":{\"indicators_suppressed\":" + HTTPManager.NO_TUTORIALS.ToString().ToLower() + ",\"indicators_confirmed\":[]},\"session_id\":\"" + ID + "\"," + (user.locked ? "\"required_action\":\"REQUIRE_VERIFIED_PHONE\"," : "") + "\"relationships\":[],\"read_state\":{\"version\":1859,\"partial\":false,\"entries\":[]},\"private_channels\":[],\"merged_members\":[],\"guilds\":[" + guildsData + "],\"guild_join_requests\":[],\"guild_experiments\":[],\"geo_ordered_rtc_regions\":[\"milan\",\"rotterdam\",\"stockholm\",\"st-pete\",\"russia\"],\"friend_suggestion_count\":0,\"experiments\":[],\"country_code\":\"IT\",\"consents\":{\"personalization\":{\"consented\":true}},\"connected_accounts\":[],\"analytics_token\":\"" + CryptoUtils.GetUniqueInt(7).ToString() + "\",\"_trace\":[\"OPEN_CORD\"]}}")), ID);
    }
}