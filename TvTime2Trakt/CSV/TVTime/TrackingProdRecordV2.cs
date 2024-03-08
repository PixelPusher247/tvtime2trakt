namespace TvTime2Trakt.CSV.TVTime;

public class TrackingProdRecordV2
{
    public string? Gsi { get; set; }
    public string? Key { get; set; }
    public string? CreatedAt { get; set; }
    public string? SeriesName { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public double SeasonId { get; set; }
    public double EpisodeId { get; set; }
    public DateTime? WatchedAt { get; set; }
}