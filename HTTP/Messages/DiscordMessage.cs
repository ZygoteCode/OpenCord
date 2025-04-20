using System.Collections.Generic;

public class DiscordMessage
{
    public string id = "";
    public int type = 0;
    public string content = "";
    public string channel_id = "";
    public MessageAuthor author;
    public List<string> attachments = new List<string>();
    public List<string> embeds = new List<string>();
    public List<string> mentions = new List<string>();
    public List<string> mention_roles = new List<string>();
    public bool pinned = false;
    public bool mention_everyone = false;
    public bool tts = false;
    public string timestamp = "";
    public string edited_timestamp = null;
    public int flags = 0;
    public List<string> components = new List<string>();
    public string nonce = "";
    public string referenced_message = null;
}