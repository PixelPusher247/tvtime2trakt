using System.Globalization;
using TraktNet;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Shows;
using TraktNet.Objects.Post.Syncs.History;
using TvTime2Trakt.CSV.FilmAffinity;
using TvTime2Trakt.CSV.TVTime;

namespace TvTime2Trakt.Trakt;

internal class TraktProcessor
{
    public static async Task AddTvTimeShowToHistoryAsync(
        TraktClient client,
        SyncProgressTracker progressTracker,
        string showName,
        ITraktShow show,
        IList<TrackingProdRecordShow> episodeList,
        IList<TrackingProdRecordV2> episodeV2List,
        bool rewatch, bool confirmationPrompt)
    {
        episodeV2List = episodeV2List.OrderBy(x => x.SeasonNumber).ThenBy(x => x.EpisodeNumber).ToList();

        foreach (var episodeV2 in episodeV2List)
        {
            var episode = episodeList.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x =>
                x.SeasonNumber == episodeV2.SeasonNumber &&
                x.EpisodeNumber == episodeV2.EpisodeNumber);
            episodeV2.WatchedAt = GetWatchedAt(episode, episodeV2);
        }

        var episodesWithDate = episodeV2List.Where(x => x.WatchedAt != null).ToList();
        var episodesWithoutDate = episodeV2List.Where(x => x.WatchedAt == null).ToList();

        var postWithoutDate = false;
        if (episodesWithoutDate.Any())
        {
            Console.WriteLine("Episodes without watched at date:");
            foreach (var episode in episodesWithoutDate)
                Console.WriteLine($"{show.Title} S{episode.SeasonNumber:D2}E{episode.EpisodeNumber:D2}");

            Console.Write("There are episodes without a watched at date, post episodes without date? (y/N)");
            postWithoutDate = Console.ReadLine()?.ToLower() == "y";
        }

        var episodesToPost = postWithoutDate ? episodeV2List : episodesWithDate;
        if (!episodesToPost.Any())
        {
            Console.WriteLine("No episodes to post.");
            progressTracker.ShowSkipped(showName, rewatch);
            return;
        }

        var traktSyncHistoryPostShows = new List<ITraktSyncHistoryPostShow>();
        var seasons = episodesToPost.GroupBy(x => x.SeasonNumber);
        traktSyncHistoryPostShows.Add(new TraktSyncHistoryPostShow
        {
            Ids = show.Ids,
            Seasons = seasons.Select(season => new TraktSyncHistoryPostShowSeason
            {
                Number = season.Key,
                Episodes = season.Select(episode => new TraktSyncHistoryPostShowEpisode
                {
                    Number = episode.EpisodeNumber,
                    WatchedAt = episode.WatchedAt
                }).Cast<ITraktSyncHistoryPostShowEpisode>().ToList()
            }).Cast<ITraktSyncHistoryPostShowSeason>().ToList()
        });

        var historyPost = new TraktSyncHistoryPost
        {
            Movies = null,
            Shows = traktSyncHistoryPostShows,
            Seasons = null,
            Episodes = null
        };

        Console.WriteLine("About to add:");
        foreach (var historyPostShow in historyPost.Shows)
            foreach (var season in historyPostShow.Seasons)
                foreach (var episode in season.Episodes)
                {
                    var episodeWatchedAt = episode.WatchedAt.HasValue
                        ? episode.WatchedAt.Value.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentUICulture)
                        : "Now";
                    Console.WriteLine($"{show.Title} S{season.Number:D2}E{episode.Number:D2} ({episodeWatchedAt})");
                }

        if (confirmationPrompt && ConfirmAndExecute(() => progressTracker.ShowSkipped(showName, rewatch)))
        {
            return;
        }

        var response = await client.Sync.AddWatchedHistoryItemsAsync(historyPost);
        if (response.IsSuccess) progressTracker.ShowSynced(showName, rewatch);
    }

    private static DateTime? GetWatchedAt(TrackingProdRecordShow? episode, TrackingProdRecordV2 episodeV2)
    {
        if (!string.IsNullOrWhiteSpace(episodeV2.CreatedAt))
        {
            return DateTimeHelper.ConvertTvTimeDate(episodeV2.CreatedAt);
        }

        if (episode is not null && !string.IsNullOrWhiteSpace(episode.CreatedAt))
            return DateTime.ParseExact(episode.CreatedAt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        return null;
    }

    public static async Task AddTvTimeMovieToHistoryAsync(TraktClient client, SyncProgressTracker progressTracker,
        string movieName,
        ITraktMovie selectedMovie,
        TrackingProdRecordMovie movie, bool rewatch, bool confirmationPrompt)
    {
        var historyPost = new TraktSyncHistoryPost
        {
            Movies = new List<ITraktSyncHistoryPostMovie>
            {
                new TraktSyncHistoryPostMovie
                {
                    Ids = selectedMovie.Ids,
                    Title = selectedMovie.Title,
                    Year = selectedMovie.Year,
                    WatchedAt = DateTime.TryParse(movie.CreatedAt, out var watchedAt) ? watchedAt : null
                }
            },
            Shows = null,
            Seasons = null,
            Episodes = null
        };


        Console.WriteLine("About to add:");
        foreach (var moviePost in historyPost.Movies)
        {
            var movieWatchedAt = moviePost.WatchedAt.HasValue
                ? moviePost.WatchedAt.Value.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentUICulture)
                : "Now";
            Console.WriteLine($"{moviePost.Title} [{moviePost.Year}] ({movieWatchedAt})");
        }

        if (confirmationPrompt && ConfirmAndExecute(() => progressTracker.MovieSkipped(Origin.TvTime, movieName, rewatch)))
        {
            return;
        }

        var response = await client.Sync.AddWatchedHistoryItemsAsync(historyPost);
        if (response.IsSuccess && !rewatch) progressTracker.MovieSynced(Origin.TvTime, movieName, rewatch);
    }

    public static async Task AddFilmAffinityMovieToHistoryAsync(
        TraktClient client,
        SyncProgressTracker progressTracker,
        ITraktMovie selectedMovie,
        FilmAffinityMovie movie,
        bool confirmationPrompt)
    {
        if (string.IsNullOrWhiteSpace(movie.Title) || string.IsNullOrWhiteSpace(movie.WatchedDate)) return;

        var historyPost = new TraktSyncHistoryPost
        {
            Movies = new List<ITraktSyncHistoryPostMovie>
            {
                new TraktSyncHistoryPostMovie
                {
                    Ids = selectedMovie.Ids,
                    Title = selectedMovie.Title,
                    Year = selectedMovie.Year,
                    WatchedAt = DateTime.TryParse(movie.WatchedDate, out var watchedAt) ? watchedAt : null
                }
            },
            Shows = null,
            Seasons = null,
            Episodes = null
        };


        Console.WriteLine("About to add:");
        foreach (var moviePost in historyPost.Movies)
        {
            var movieWatchedAt = moviePost.WatchedAt.HasValue
                ? moviePost.WatchedAt.Value.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentUICulture)
                : "Now";
            Console.WriteLine($"{moviePost.Title} [{moviePost.Year}] ({movieWatchedAt})");
        }

        if (confirmationPrompt && ConfirmAndExecute(() => progressTracker.MovieSkipped(Origin.FilmAffinity, movie.Title, false)))
        {
            return;
        }

        var response = await client.Sync.AddWatchedHistoryItemsAsync(historyPost);
        if (response.IsSuccess) progressTracker.MovieSynced(Origin.FilmAffinity, movie.Title, false);
    }

    private static bool ConfirmAndExecute(Action action)
    {
        Console.Write("Press y to confirm: ");
        if (Console.ReadKey(true).Key != ConsoleKey.Y)
        {
            Console.WriteLine("Skipping.");
            action();
            return true;
        }

        return false;
    }
}