public class WebSocketUser
{
    public string id = null, ip = null, token = null;
    public bool logged = false;
    public ZlibStreamContext zlibStreamContext = null;
    public int sequence = 0;
    public RegisteredUser user = null;
}