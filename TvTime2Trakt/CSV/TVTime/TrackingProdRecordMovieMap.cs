using CsvHelper.Configuration;

namespace TvTime2Trakt.CSV.TVTime;

public sealed class TrackingProdRecordMovieMap : ClassMap<TrackingProdRecordMovie>
{
    public TrackingProdRecordMovieMap()
    {
        Map(m => m.Type).Name("type");
        Map(m => m.CreatedAt).Name("created_at");
        Map(m => m.RewatchCount).Name("watch_count").Default(0);
        Map(m => m.MovieName).Name("movie_name");
        Map(m => m.ReleaseDate).Name("release_date");
        Map(m => m.EntityType).Name("entity_type");
        Map(m => m.AlphaRangeKey).Name("alpha_range_key");
    }
}