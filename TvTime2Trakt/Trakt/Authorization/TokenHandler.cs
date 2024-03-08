using Newtonsoft.Json;

namespace TvTime2Trakt.Trakt.Authorization;

internal class TokenHandler
{
    public static async Task<TokenData?> GetTokenDataAsync()
    {
        if (!File.Exists("token.json")) return null;

        var json = await File.ReadAllTextAsync("token.json");
        return JsonConvert.DeserializeObject<TokenData>(json);
    }

    public static async Task SaveTokenDataAsync(TokenData tokenData)
    {
        var json = JsonConvert.SerializeObject(tokenData);
        await File.WriteAllTextAsync("token.json", json);
    }
}