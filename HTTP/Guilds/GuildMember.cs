using System.Collections.Generic;

public class GuildMember
{
    public GuildUser user;
    public List<string> roles = new List<string>();
    public string premium_since = null;
    public bool pending = false;
    public string nick = null;
    public bool mute = false;
    public string joined_at = "";
    public string hoisted_role = null;
    public bool deaf = false;
    public string communication_disabled_until = null;
    public string avatar = null;
}