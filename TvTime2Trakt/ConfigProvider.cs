using Microsoft.Extensions.Configuration;

namespace TvTime2Trakt;

internal class ConfigProvider
{
    public static Configuration? GetConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("config.json", true, true)
#if DEBUG
            .AddJsonFile("config.local.json", true, true)
#endif
            .Build();

        return configurationBuilder.Get<Configuration>();
    }
}