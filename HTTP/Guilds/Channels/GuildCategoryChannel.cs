using System.Collections.Generic;

public class GuildCategoryChannel : GuildChannel
{
    public int type = 4;
    public int position = 0;
    public List<string> permission_overwrites = new List<string>();
    public string parent_id = null;
    public bool nsfw = false;
    public string name = "";
    public string id = "";
}