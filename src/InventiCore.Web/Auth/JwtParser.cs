using System.Security.Claims;
using System.Text.Json;

namespace InventiCore.Web.Auth;

public static class JwtParser
{
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
            }
        }
        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
