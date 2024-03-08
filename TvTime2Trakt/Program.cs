using TvTime2Trakt;

var config = ConfigProvider.GetConfiguration();

if (config == null)
{
    Console.WriteLine("Configuration could not be loaded.");
}
else
{
    var dataProcessor = new DataProcessor(config);
    await dataProcessor.ProcessDataAsync();
}

Console.WriteLine("Press any key to exit.");
Console.ReadKey();