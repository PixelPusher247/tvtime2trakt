using CsvHelper.Configuration;

namespace TvTime2Trakt.CSV.TVTime;

public sealed class TrackingProdRecordV2Map : ClassMap<TrackingProdRecordV2>
{
    public TrackingProdRecordV2Map()
    {
        Map(m => m.Gsi).Name("gsi");
        Map(m => m.Key).Name("key");
        Map(m => m.CreatedAt).Name("created_at");
        Map(m => m.SeriesName).Name("series_name");
        Map(m => m.SeasonNumber).Name("season_number");
        Map(m => m.EpisodeNumber).Name("episode_number").Default(null);
        Map(m => m.SeasonId).Name("s_id").Default(null);
        Map(m => m.EpisodeId).Name("episode_id").Default(null);
    }
}