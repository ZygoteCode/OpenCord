using System;
using System.Text;

public class SCUnlock
{
    public static void Process(string[] arguments)
    {
        RegisteredUser user = null;
        WebSocketUser wsUser = null;
        Tuple<RegisteredUser, WebSocketUser> tuple = DataManager.GetUserByID(arguments[0]);
        user = tuple.Item1;
        wsUser = tuple.Item2;

        if (user == null)
        {
            Console.WriteLine("[!] User not found.");
            return;
        }

        user.locked = true;
        DataManager.SaveUsers();

        if (wsUser != null)
        {
            ZlibStreamContext zlibContext = wsUser.zlibStreamContext;
            wsUser.sequence++;
            HTTPManager.wsManager.SendTo(zlibContext.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"USER_REQUIRED_ACTION_UPDATE\",\"s\":" + wsUser.sequence + ",\"op\":0,\"d\":{\"required_action\":null}}")), wsUser.id);
        }

        Console.WriteLine("[!] Succesfully remove phone lock this OpenCord user.");
    }
}