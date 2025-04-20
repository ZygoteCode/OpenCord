using System;
using Newtonsoft.Json;
using System.Text;

public class SCAllBadges
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

        user.flags = Utils.GetSuperUserFlags();
        user.public_flags = Utils.GetSuperUserFlags();
        user.hypesquad = true;
        DataManager.SaveUsers();

        if (wsUser != null)
        {
            ZlibStreamContext zlibContext = wsUser.zlibStreamContext;
            string userJson = JsonConvert.SerializeObject(user);
            wsUser.sequence++;
            HTTPManager.wsManager.SendTo(zlibContext.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"USER_UPDATE\",\"s\":4,\"op\":0,\"d\":" + userJson + "}")), wsUser.id);
        }

        Console.WriteLine("[!] Succesfully gave all OpenCord badges to this user.");
    }
}