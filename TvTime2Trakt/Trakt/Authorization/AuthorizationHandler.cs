using TraktNet;
using TraktNet.Exceptions;
using TraktNet.Objects.Authentication;

namespace TvTime2Trakt.Trakt.Authorization;

internal class AuthorizationHandler
{
    public static async Task<bool> EnsureAuthorizationAsync(TraktClient client)
    {
        try
        {
            var tokenData = await TokenHandler.GetTokenDataAsync() ?? await AuthenticateAsync(client);
            if (tokenData is null)
            {
                Console.WriteLine("Authorization could not be loaded.");
                return false;
            }

            if (tokenData.CreatedAt.AddSeconds(tokenData.ExpiresInSeconds) < DateTime.UtcNow)
            {
                var newAuthorization = await RefreshAuthorizationAsync(client, tokenData);
                if (newAuthorization is not null)
                {
                    tokenData = new TokenData(newAuthorization.AccessToken,
                        newAuthorization.RefreshToken,
                        newAuthorization.ExpiresInSeconds,
                        newAuthorization.CreatedAt);
                    await TokenHandler.SaveTokenDataAsync(tokenData);
                }
            }
            else
            {
                await client.Authentication.RefreshAuthorizationAsync(tokenData.RefreshToken);
            }

            return true;
        }
        catch (TraktException)
        {
            return await AuthenticateAsync(client) is not null;
        }
    }

    private static async Task<TokenData?> AuthenticateAsync(TraktClient client)
    {
        var authorization = await GetAuthorizationAsync(client);
        if (authorization is not null)
        {
            var tokenData = new TokenData(authorization.AccessToken,
                authorization.RefreshToken,
                authorization.ExpiresInSeconds,
                authorization.CreatedAt);
            await TokenHandler.SaveTokenDataAsync(tokenData);
            return tokenData;
        }

        return null;
    }

    internal static async Task<ITraktAuthorization?> GetAuthorizationAsync(TraktClient client)
    {
        var authorizationUrl = client.Authentication.CreateAuthorizationUrl();

        if (string.IsNullOrEmpty(authorizationUrl)) return null;

        Console.WriteLine("You have to authenticate this application.");
        Console.WriteLine("Please visit the following webpage:");
        Console.WriteLine($"{authorizationUrl}\n");

        Console.WriteLine("Enter the PIN code from Trakt.tv:");
        var code = Console.ReadLine();

        if (string.IsNullOrEmpty(code)) return null;

        var authorizationResponse = await client.Authentication.GetAuthorizationAsync(code);
        var authorization = authorizationResponse.Value;

        if (!authorization.IsValid) return null;

        Console.WriteLine("-------------- Authentication successful --------------");
        WriteAuthorizationInformation(authorization);
        Console.WriteLine("-------------------------------------------------------");

        return authorization;
    }

    internal static async Task<ITraktAuthorization?> RefreshAuthorizationAsync(TraktClient client,
        TokenData tokenData)
    {
        var newAuthorizationResponse = await client.Authentication.RefreshAuthorizationAsync(tokenData.RefreshToken);

        var newAuthorization = newAuthorizationResponse.Value;

        if (!newAuthorization.IsValid) return null;

        Console.WriteLine("-------------- Refresh successful --------------");
        WriteAuthorizationInformation(newAuthorization);
        Console.WriteLine("-------------------------------------------------------");

        return newAuthorization;
    }

    private static void WriteAuthorizationInformation(ITraktAuthorization authorization)
    {
        Console.WriteLine($"Created (UTC): {authorization.CreatedAt}");
        Console.WriteLine($"Access Scope: {authorization.Scope.DisplayName}");
        Console.WriteLine($"Refresh Possible: {authorization.IsRefreshPossible}");
        Console.WriteLine($"Valid: {authorization.IsValid}");
        Console.WriteLine($"Access Token: {authorization.AccessToken}");
        Console.WriteLine($"Refresh Token: {authorization.RefreshToken}");
        Console.WriteLine($"Token Expired: {authorization.IsExpired}");

        var created = authorization.CreatedAt;
        var expirationDate = created.AddSeconds(authorization.ExpiresInSeconds);
        var difference = expirationDate - DateTime.UtcNow;

        var days = difference.Days > 0 ? difference.Days : 0;
        var hours = difference.Hours > 0 ? difference.Hours : 0;
        var minutes = difference.Minutes > 0 ? difference.Minutes : 0;

        Console.WriteLine($"Expires in {days} Days, {hours} Hours, {minutes} Minutes");
    }
}