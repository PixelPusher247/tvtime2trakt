namespace TvTime2Trakt.Trakt.Authorization;

public record TokenData(string AccessToken, string RefreshToken, uint ExpiresInSeconds, DateTime CreatedAt)
{
    public override string ToString()
    {
        return $"{{ AccessToken = {AccessToken}, RefreshToken = {RefreshToken}, ExpiresInSeconds = {ExpiresInSeconds}, CreatedAt = {CreatedAt} }}";
    }
}