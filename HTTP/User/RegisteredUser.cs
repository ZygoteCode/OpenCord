using System.Collections.Generic;

public class RegisteredUser
{
    public string id = null, username = null, avatar = null, discriminator = null, banner = null, bio = null, locale = null, email = null, phone = null, password = null, token = null, date_of_birth = null, ip = null, superProperties = null, deviceUUID = null;
    public int public_flags = -1, flags = -1, purchased_flags = -1, accent_color = -1, banner_color = -1, premium_type = -1;
    public bool nsfw_allowed = false, mfa_enabled = false, verified = false, premium = false, hypesquad = false, disabled = false, locked = false;
    public long timestamp = -1, premium_since = 0;
    public UserSettings settings = null;
    public UserFlags userFlags = null;
}