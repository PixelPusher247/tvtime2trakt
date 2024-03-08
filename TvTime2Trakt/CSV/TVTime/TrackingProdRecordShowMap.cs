using CsvHelper.Configuration;

namespace TvTime2Trakt.CSV.TVTime;

public sealed class TrackingProdRecordShowMap : ClassMap<TrackingProdRecordShow>
{
    public TrackingProdRecordShowMap()
    {
        Map(m => m.Type).Name("type");
        Map(m => m.CreatedAt).Name("created_at");
        Map(m => m.SeriesName).Name("series_name");
        Map(m => m.EntityType).Name("entity_type");
        Map(m => m.EpisodeNumber).Name("episode_number");
        Map(m => m.SeasonNumber).Name("season_number");
    }
}