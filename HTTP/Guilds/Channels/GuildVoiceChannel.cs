using System.Collections.Generic;

public class GuildVoiceChannel : GuildChannel
{
    public int user_limit = 0;
    public int type = 2;
    public string rtc_region = null;
    public int position = 0;
    public List<string> permission_overwrites = new List<string>();
    public string parent_id = null;
    public bool nsfw = false;
    public string name = "";
    public string last_message_id = null;
    public string id = "";
    public int bitrate = 96000;
}