namespace TvTime2Trakt;

public class Configuration
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TvTimeDataFolder { get; set; }
    public string? FilmAffinityExportFile { get; set; }
    public string? ProgressFile { get; set; }
    public bool IgnoreSkippedShows { get; set; }
    public bool IgnoreSkippedMovies { get; set; }
    public bool ConfirmationPromptBeforePost { get; set; }
}