using Newtonsoft.Json.Linq;

public class OpenCordToken
{
    public string token = null, id = null, email = null, password = null, date_of_birth = null, timestamp = null, ip = null, deviceUUID = null;

    public OpenCordToken(string token, RegisteredUser user)
    {
        this.token = token;
        this.id = user.id;
        this.email = user.email;
        this.password = user.password;
        this.date_of_birth = user.date_of_birth;
        this.timestamp = user.timestamp.ToString();
        this.ip = user.ip;
        this.deviceUUID = user.deviceUUID;
    }

    public bool IsValid()
    {
        try
        {
            return token == CryptoUtils.GetMD5(CryptoUtils.EncryptAES256("{\"id\":\"" + id + "\",\"email\":\"" + email + "\",\"password\":\"" + password + "\",\"date_of_birth\":\"" + date_of_birth + "\",\"timestamp\":\"" + timestamp + "\",\"ip\":\"" + ip + "\",\"deviceUUID\":\"" + this.deviceUUID + "\"}", "OPEN_CORD_932123"));
        }
        catch
        {
            return false;
        }
    }
}