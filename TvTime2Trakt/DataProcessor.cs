using TvTime2Trakt.CSV.FilmAffinity;
using TvTime2Trakt.CSV.TVTime;
using TvTime2Trakt.CSV;
using TvTime2Trakt.Trakt.Authorization;
using TvTime2Trakt.Trakt;
using TraktNet;

namespace TvTime2Trakt;

internal class DataProcessor
{
    private readonly TraktClient _client;
    private readonly SyncProgressTracker _progressTracker;
    private readonly Configuration _config;

    private DateTime _lastPostTime = DateTime.MinValue;

    public DataProcessor(Configuration config)
    {
        _config = config;
        _client = new TraktClient(config.ClientId, config.ClientSecret);

        var path = !string.IsNullOrWhiteSpace(config.ProgressFile) ? config.ProgressFile : "progress.json";
        _progressTracker = new SyncProgressTracker(path);
    }

    public async Task ProcessDataAsync()
    {
        var result = await AuthorizationHandler.EnsureAuthorizationAsync(_client);

        if (result)
        {
            Console.WriteLine("Authorization successful.");
            if (Directory.Exists(_config.TvTimeDataFolder))
                await ProcessTvTimeDataAsync();
            else
                Console.WriteLine("TV Time data folder not found.");

            if (!string.IsNullOrWhiteSpace(_config.FilmAffinityExportFile))
            {
                if (File.Exists(_config.FilmAffinityExportFile))
                    await ProcessFilmAffinityDataAsync();
                else
                    Console.WriteLine("FilmAffinity export file not found.");
            }
        }
        else
        {
            Console.WriteLine("Authorization failed.");
            return;
        }

        Console.WriteLine("All done!");
    }

    private async Task ProcessFilmAffinityDataAsync()
    {
        ArgumentNullException.ThrowIfNull(_progressTracker);
        ArgumentNullException.ThrowIfNull(_config.FilmAffinityExportFile);

        var movies = CsvSafeReader
            .ReadRecordsFromFile<FilmAffinityMovie, FilmAffinityMovieMap>(_config.FilmAffinityExportFile).ToList();
        foreach (var movieWatch in movies)
            if (movieWatch.Title is not null
                && !_progressTracker.IsMovieSynced(Origin.FilmAffinity, movieWatch.Title, false)
                && (!_config.IgnoreSkippedMovies ||
                    !_progressTracker.IsMovieSkipped(Origin.FilmAffinity, movieWatch.Title, false)))
                await ProcessFilmAffinityMovieAsync(movieWatch);
    }

    private async Task ProcessTvTimeDataAsync()
    {
        ArgumentNullException.ThrowIfNull(_config.TvTimeDataFolder);

        var trackingProdRecordsFile = Path.Combine(_config.TvTimeDataFolder, "tracking-prod-records.csv");
        if (!File.Exists(trackingProdRecordsFile))
        {
            Console.WriteLine("tracking-prod-records.csv not found.");
            return;
        }

        var trackingProdRecordsV2File = Path.Combine(_config.TvTimeDataFolder, "tracking-prod-records-v2.csv");
        if (!File.Exists(trackingProdRecordsV2File))
        {
            Console.WriteLine("tracking-prod-records-v2.csv not found.");
            return;
        }

        var showRecordList = CsvSafeReader
            .ReadRecordsFromFile<TrackingProdRecordShow, TrackingProdRecordShowMap>(trackingProdRecordsFile)
            .Where(x => x.Type == "watch" && x.EntityType == "episode" && !string.IsNullOrWhiteSpace(x.SeriesName))
            .OrderBy(x => x.SeriesName).ToList();
        var showRecordV2List = CsvSafeReader
            .ReadRecordsFromFile<TrackingProdRecordV2, TrackingProdRecordV2Map>(trackingProdRecordsV2File)
            .OrderBy(x => x.SeriesName).ToList();
        await ProcessTvShowsAsync(showRecordList, showRecordV2List);

        var records = CsvSafeReader
            .ReadRecordsFromFile<TrackingProdRecordMovie, TrackingProdRecordMovieMap>(trackingProdRecordsFile)
            .Where(x => x.Type is "watch" or "rewatch" && x.EntityType == "movie" &&
                        !string.IsNullOrWhiteSpace(x.MovieName)).OrderBy(x => x.MovieName).ToList();
        await ProcessMoviesAsync(records);
    }

    private async Task ProcessTvShowsAsync(IList<TrackingProdRecordShow> recordList, IList<TrackingProdRecordV2> recordV2List)
    {
        var tvShowWatchs = recordV2List.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Key.StartsWith("watch"))
            .ToList();
        var tvShowRewatchs = recordV2List.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Key.StartsWith("rewatch"))
            .ToList();

        var groupedTvShowWatchs = tvShowWatchs.GroupBy(x => x.SeriesName).ToList();
        var groupedTvShowRewatchs = tvShowRewatchs.GroupBy(x => x.SeriesName).ToList();

        foreach (var tvShow in groupedTvShowWatchs)
            if (!string.IsNullOrWhiteSpace(tvShow.Key)
                && !_progressTracker.IsShowSynced(tvShow.Key, false)
                && (!_config.IgnoreSkippedShows || !_progressTracker.IsShowSkipped(tvShow.Key, false)))
            {
                var episodes = recordList.Where(x => x.SeriesName == tvShow.Key).ToList();
                await ProcessShowGroupAsync(tvShow.Key, episodes, tvShow.ToList(), false);
            }

        Console.WriteLine();

        foreach (var tvShowRewatch in groupedTvShowRewatchs)
            if (!string.IsNullOrWhiteSpace(tvShowRewatch.Key)
                && !_progressTracker.IsShowSynced(tvShowRewatch.Key, true)
                && (!_config.IgnoreSkippedShows || !_progressTracker.IsShowSkipped(tvShowRewatch.Key, true)))
                await ProcessShowGroupAsync(tvShowRewatch.Key, new List<TrackingProdRecordShow>(),
                    tvShowRewatchs.ToList(),
                    true);

        Console.WriteLine();
    }

    private async Task ProcessShowGroupAsync(string showName, IList<TrackingProdRecordShow> episodeList, IList<TrackingProdRecordV2> episodeV2List, bool rewatch)
    {
        Console.WriteLine($"TV Show: {showName}");
        var searchResults = await TraktSearcher.GetShowsByNameAsync(_client, showName);

        var selectedShow = searchResults.Count switch
        {
            1 => searchResults.First(),
            > 1 => TraktSearcher.FilterWithManualInput(searchResults),
            _ => null
        };

        if (selectedShow is null)
        {
            Console.Write($"No show found for {showName}. Input trakt show id manually: ");
            var manualShowId = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(manualShowId))
            {
                Console.WriteLine("No show id provided. Skipping show.");
                _progressTracker.ShowSkipped(showName, rewatch);
                return;
            }

            selectedShow = await TraktSearcher.GetShowByTraktIdAsync(_client, manualShowId);
        }

        if (selectedShow is null)
        {
            Console.WriteLine($"No show found for {showName}.");
            return;
        }

        Console.WriteLine($"TV Show selected: {selectedShow.Title} | https://trakt.tv/shows/{selectedShow.Ids.Trakt}");
        await TraktProcessor.AddTvTimeShowToHistoryAsync(_client, _progressTracker, showName, selectedShow, episodeList,
            episodeV2List,
            rewatch, _config.ConfirmationPromptBeforePost);
        await RateLimitSafeguard();
    }

    private async Task ProcessMoviesAsync(IList<TrackingProdRecordMovie> records)
    {
        var movieWatches = records.Where(x => x.Type == "watch").ToList();
        var movieRewatchs = records.Where(x => x.Type == "rewatch").ToList();

        Console.WriteLine($"Movie Watchs: {movieWatches.Count}");
        foreach (var movieWatch in movieWatches)
            if (!string.IsNullOrWhiteSpace(movieWatch.MovieName)
                && !_progressTracker.IsMovieSynced(Origin.TvTime, movieWatch.MovieName, false)
                && (!_config.IgnoreSkippedMovies ||
                    !_progressTracker.IsMovieSkipped(Origin.TvTime, movieWatch.MovieName, false)))
                await ProcessMovieAsync(movieWatch, false);

        Console.WriteLine();

        Console.WriteLine($"Movie Rewatchs: {movieRewatchs.Count}");
        foreach (var movieRewatch in movieRewatchs)
            if (!string.IsNullOrWhiteSpace(movieRewatch.MovieName)
                && !_progressTracker.IsMovieSynced(Origin.TvTime, movieRewatch.MovieName, true)
                && (!_config.IgnoreSkippedMovies ||
                    !_progressTracker.IsMovieSkipped(Origin.TvTime, movieRewatch.MovieName, true)))
                await ProcessMovieAsync(movieRewatch, true);
    }

    private async Task ProcessMovieAsync(TrackingProdRecordMovie movie, bool rewatch)
    {
        if (string.IsNullOrWhiteSpace(movie.MovieName)) return;

        int? year = null;
        if (!string.IsNullOrWhiteSpace(movie.ReleaseDate) && DateTime.TryParse(movie.ReleaseDate, out var dateValue))
            year = dateValue.Year;

        var movieString = $"Movie: {movie.MovieName}";
        if (year.HasValue) movieString += $" ({year})";

        Console.WriteLine(movieString);
        var searchResults = await TraktSearcher.GetMoviesByNameAsync(_client, movie.MovieName, year);

        var selectedMovie = searchResults.Count switch
        {
            1 => searchResults.First(),
            > 1 => TraktSearcher.FilterWithManualInput(searchResults),
            _ => null
        };

        if (selectedMovie is null)
        {
            Console.Write(
                "No movie found. Input trakt movie id manually ('ignore' to mark as done, 'skip' to mark as skipped): ");
            var manualMovieId = Console.ReadLine();

            if (manualMovieId == "ignore")
            {
                _progressTracker.MovieSynced(Origin.FilmAffinity, movie.MovieName, rewatch);
                return;
            }

            if (manualMovieId == "skip")
            {
                _progressTracker.MovieSkipped(Origin.FilmAffinity, movie.MovieName, rewatch);
                return;
            }

            selectedMovie = await TraktSearcher.GetMovieByTraktIdAsync(_client, manualMovieId);
        }

        if (selectedMovie is null)
        {
            Console.WriteLine("Movie not found");
            return;
        }

        Console.WriteLine($"Movie selected: {selectedMovie.Title} | https://trakt.tv/movies/{selectedMovie.Ids.Trakt}");
        await TraktProcessor.AddTvTimeMovieToHistoryAsync(_client, _progressTracker, movie.MovieName, selectedMovie,
            movie,
            rewatch,
            _config.ConfirmationPromptBeforePost);
        await RateLimitSafeguard();
    }

    private async Task ProcessFilmAffinityMovieAsync(FilmAffinityMovie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Title)) return;

        Console.WriteLine($"Movie: {movie.Title} ({movie.Year})");
        var searchResults = await TraktSearcher.GetMoviesByNameAsync(_client, movie.Title, movie.Year);

        var selectedMovie = searchResults.Count switch
        {
            1 => searchResults.First(),
            > 1 => TraktSearcher.FilterWithManualInput(searchResults),
            _ => null
        };

        if (selectedMovie is null)
        {
            Console.Write(
                "No movie found. Input trakt movie id manually ('ignore' to mark as done, 'skip' to mark as skipped): ");
            var manualMovieId = Console.ReadLine();
            if (manualMovieId == "ignore")
            {
                _progressTracker.MovieSynced(Origin.FilmAffinity, movie.Title, false);
                return;
            }

            if (manualMovieId == "skip")
            {
                _progressTracker.MovieSkipped(Origin.FilmAffinity, movie.Title, false);
                return;
            }

            selectedMovie = await TraktSearcher.GetMovieByTraktIdAsync(_client, manualMovieId);
        }

        if (selectedMovie is null)
        {
            Console.WriteLine("Movie not found");
            return;
        }

        Console.WriteLine($"Movie selected: {selectedMovie.Title} | https://trakt.tv/movies/{selectedMovie.Ids.Trakt}");
        await TraktProcessor.AddFilmAffinityMovieToHistoryAsync(_client, _progressTracker, selectedMovie, movie,
            _config.ConfirmationPromptBeforePost);
        await RateLimitSafeguard();
    }

    private async Task RateLimitSafeguard()
    {
        var timeSinceLastPost = DateTime.UtcNow - _lastPostTime;
        var minInterval = TimeSpan.FromMilliseconds(1500);

        if (timeSinceLastPost < minInterval)
        {
            var delay = minInterval - timeSinceLastPost;
            Console.WriteLine($"API Rate limit safeguard: waiting {delay.TotalSeconds:F1} seconds");
            await Task.Delay(delay);
        }

        _lastPostTime = DateTime.UtcNow;
    }
}