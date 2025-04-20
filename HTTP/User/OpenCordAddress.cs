public class OpenCordAddress
{
    public string ip = null;
    public long loginRatelimit = -1, registerRatelimit = -1, loginRequests = 0, registerRequests = 0;
    public long sendMessageRateLimit = -1, sendMessageRequests = 0, sendMessageTime = 0;
    public long createGuildRateLimit = -1, createGuildRequests = 0, createGuildTime = 0;
    public long getMessagesRateLimit = -1, getMessagesRequests = 0, getMessagesTime = 0;
    public long getOwnUserRateLimit = -1, getOwnUserRequests = 0, getOwnUserTime = 0;
    public long updateSettingsRateLimit = -1, updateSettingsRequests = 0, updateSettingsTime = 0;
    public long getFingerprintRateLimit = -1, getFingerprintRequests = 0, getFingerprintTime = 0;
}