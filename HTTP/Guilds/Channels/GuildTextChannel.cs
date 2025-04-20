using System.Collections.Generic;

public class GuildTextChannel : GuildChannel
{
    public int type = 0;
    public string topic = null;
    public int rate_limit_per_user = 0;
    public int position = 0;
    public List<string> permission_overwrites = new List<string>();
    public string parent_id = null;
    public bool nsfw = false;
    public string name = "";
    public string last_message_id = null;
    public string id = "";
}