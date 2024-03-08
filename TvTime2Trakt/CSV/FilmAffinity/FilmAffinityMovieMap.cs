using CsvHelper.Configuration;

namespace TvTime2Trakt.CSV.FilmAffinity;

public sealed class FilmAffinityMovieMap : ClassMap<FilmAffinityMovie>
{
    public FilmAffinityMovieMap()
    {
        Map(m => m.Title).Name("Title");
        Map(m => m.Year).Name("Year");
        Map(m => m.WatchedDate).Name("WatchedDate");
    }
}