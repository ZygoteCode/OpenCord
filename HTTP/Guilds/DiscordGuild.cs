using System.Collections.Generic;

public class DiscordGuild
{
    public string owner_id = "";
    public string hub_type = null;
    public bool large = false;
    public string name = "";
    public string discovery_splash = null;
    public string joined_at = "";
    public int verification_level = 0;
    public string description = "";
    public string vanity_url_code = null;
    public string application_id = null;
    public int max_members = 250000;
    public int member_count = 1;
    public string region = "deprecated";
    public string public_updates_channel_id = null;
    public bool nsfw = false;
    public int mfa_level = 0;
    public int max_video_channel_users = 25;
    public int afk_timeout = 300;
    public int premium_subscription_count = 0;
    public string rules_channel_id = null;
    public string system_channel_id = null;
    public int application_command_count = 0;
    public string splash = null;
    public int explicit_content_filter = 0;
    public string id = "";
    public string banner = null;
    public string icon = null;
    public int nsfw_level = 0;
    public int default_message_notifications = 0;
    public string afk_channel_id = null;
    public int system_channel_flags = 0;
    public bool premium_progress_bar_enabled = false;
    public bool lazy = true;
    public int premium_tier = 0;
    public string preferred_locale = "en-US";
    public List<GuildRole> roles = new List<GuildRole>();
    public GuildMember firstMember;
    public List<object> channels = new List<object>();
    public ResourceSemaphore semaphore = new ResourceSemaphore();
    public List<string> guildMembers = new List<string>();
}