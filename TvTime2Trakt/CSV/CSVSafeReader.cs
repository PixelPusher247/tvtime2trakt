using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TvTime2Trakt.CSV;

public static class CsvSafeReader
{
    public static List<T> ReadRecordsFromFile<T, TMap>(string filePath)
        where T : class
        where TMap : ClassMap
    {
        var records = new List<T>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            BadDataFound = _ => { },
            MissingFieldFound = null,
            HeaderValidated = null,
            ReadingExceptionOccurred = _ => false,
        });

        csv.Context.RegisterClassMap<TMap>();

        while (csv.Read())
        {
            T? record;
            try
            {
                record = csv.GetRecord<T>();
            }
            catch (TypeConverterException)
            {
                continue;
            }

            if (record != null) records.Add(record);
        }

        return records;
    }
}