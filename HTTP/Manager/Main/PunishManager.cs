using System.Text;
using Newtonsoft.Json;
using System.Threading;

public class PunishManager
{
    public static void KickUser(RegisteredUser user, WebSocketUser wsUser)
    {
        user.token = null;
        HTTPManager.wsManager.CloseSession(wsUser.id);
    }

    public static void BanUser(RegisteredUser user, WebSocketUser wsUser)
    {
        HTTPManager.wsManager.CloseSession(wsUser.id);
        HTTPManager.registeredUsers.Remove(user);
        DataManager.SaveUsers();
    }

    public static void BanUser(OpenCordAddress address, RegisteredUser user, WebSocketUser wsUser)
    {
        BanIPAddress(address.ip);
        HTTPManager.wsManager.CloseSession(wsUser.id);
        user.disabled = true;
        DataManager.SaveUsers();
        LockUser(user, wsUser);
    }

    public static void LockUser(RegisteredUser user, WebSocketUser wsUser)
    {
        ZlibStreamContext zlibContext = wsUser.zlibStreamContext;
        user.locked = true;
        wsUser.sequence++;
        HTTPManager.wsManager.SendTo(zlibContext.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"USER_REQUIRED_ACTION_UPDATE\",\"s\":" + wsUser.sequence + ",\"op\":0,\"d\":{\"required_action\":\"REQUIRE_VERIFIED_PHONE\"}}")), wsUser.id);
        DataManager.SaveUsers();
    }

    public static void UnlockUser(RegisteredUser user, WebSocketUser wsUser)
    {
        ZlibStreamContext zlibContext = wsUser.zlibStreamContext;
        user.locked = false;
        user.verified = true;
        wsUser.sequence++;
        HTTPManager.wsManager.SendTo(zlibContext.Deflate(Encoding.UTF8.GetBytes("{\"t\":\"USER_REQUIRED_ACTION_UPDATE\",\"s\":" + wsUser.sequence + ",\"op\":0,\"d\":{\"required_action\":null}}")), wsUser.id);
        DataManager.SaveUsers();
    }

    public static void BanIPAddress(string ip)
    {
        while (HTTPManager.bannedIpsSemaphore.IsResourceNotAvailable())
        {
            Thread.Sleep(HTTPManager.SEMAPHORE_SLEEP);
        }

        if (HTTPManager.bannedIpsSemaphore.LockResource())
        {
            HTTPManager.bannedIpAddresses.Add(ip);

            try
            {
                System.IO.File.WriteAllText("data\\banned_ips.json", JsonConvert.SerializeObject(HTTPManager.bannedIpAddresses));
            }
            catch
            {

            }

            HTTPManager.bannedIpsSemaphore.UnlockResource();
        }
    }
}