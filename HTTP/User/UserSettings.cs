using System.Collections.Generic;

public class UserSettings
{
    public string status = "online", theme = "dark";
    public int afk_timeout = 600, explicit_content_filter = 1, timezone_offset = -60, friend_discovery_flags = 0, animate_stickers = 0;
    public bool inline_attachment_media = true, show_current_game = true, view_nsfw_guilds = false, enable_tts_command = true, render_reactions = true, gif_auto_play = true, stream_notifications_enabled = true, animate_emoji = true, detect_platform_accounts = true, default_guilds_restricted = false, allow_accessibility_detection = false, native_phone_integration_enabled = true, contact_sync_enabled = false, disable_games_tab = true, inline_embed_media = true, developer_mode = false, render_embeds = true, message_display_compact = false, convert_emoticons = true, passwordless = true;
    public FriendSourceFlags friend_source_flags;
}