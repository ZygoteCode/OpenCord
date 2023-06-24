using System;
using System.Text;
using Newtonsoft.Json;

public class SCRemoveNitro
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

        user.purchased_flags = 0;
        user.premium = false;
        user.premium_type = 0;
        user.premium_since = 0;
        DataManager.SaveUsers();

        if (wsUser != null)
        {
            ZlibStreamContext zlibContext = wsUser.zlibStreamContext;
            string userJson = JsonConvert.SerializeObject(user);
            wsUser.sequence++;
            HTTPManager.wsManager.SendTo(zlibContext.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"USER_UPDATE\",\"s\":" + wsUser.sequence + ",\"op\":0,\"d\":" + userJson + "}")), wsUser.id);
        }

        Console.WriteLine("[!] Succesfully removed Nitro from this OpenCord user.");
    }
}