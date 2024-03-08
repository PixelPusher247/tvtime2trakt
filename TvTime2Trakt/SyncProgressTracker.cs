using System.Text.Json;

namespace TvTime2Trakt;

public class SyncProgressTracker
{
    private readonly string _path;

    public SyncProgressTracker(string path)
    {
        _path = path;
    }

    public bool IsShowSynced(string showName, bool rewatch)
    {
        var progress = GetProgress();

        if (rewatch) return progress != null && progress.ShowRewatchs.Contains(showName);

        return progress != null && progress.Shows.Contains(showName);
    }

    public bool IsShowSkipped(string showName, bool rewatch)
    {
        var progress = GetProgress();
        if (rewatch) return progress != null && progress.SkippedShowRewatchs.Contains(showName);

        return progress != null && progress.SkippedShows.Contains(showName);
    }

    public void ShowSynced(string showName, bool rewatch)
    {
        var progress = GetProgress() ?? new Progress();
        
        if (rewatch)
        {
            progress.SkippedShowRewatchs.Remove(showName);
            progress.ShowRewatchs.Add(showName);
        }
        else
        {
            progress.SkippedShows.Remove(showName);
            progress.Shows.Add(showName);
        }

        SaveProgress(progress);
    }

    public void ShowSkipped(string showName, bool rewatch)
    {
        var progress = GetProgress() ?? new Progress();
        if (rewatch)
            progress.SkippedShowRewatchs.Add(showName);
        else
            progress.SkippedShows.Add(showName);

        SaveProgress(progress);
    }

    public bool IsMovieSynced(Origin origin, string movieName, bool rewatch)
    {
        var progress = GetProgress();

        if (rewatch) return progress != null && progress.MovieRewatchs.Contains(movieName);

        if (origin == Origin.FilmAffinity) return progress != null && progress.FilmAffinityMovies.Contains(movieName);

        return progress != null && progress.Movies.Contains(movieName);
    }

    public bool IsMovieSkipped(Origin origin, string movieName, bool rewatch)
    {
        var progress = GetProgress();
        if (rewatch) return progress != null && progress.SkippedMovieRewatchs.Contains(movieName);

        if (origin == Origin.FilmAffinity) return progress != null && progress.FilmAffinitySkippedMovies.Contains(movieName);

        return progress != null && progress.SkippedMovies.Contains(movieName);
    }

    public void MovieSynced(Origin origin, string movieName, bool rewatch)
    {
        var progress = GetProgress() ?? new Progress();
        if (rewatch)
        {
            progress.SkippedMovieRewatchs.Remove(movieName);
            progress.MovieRewatchs.Add(movieName);
        }
        else
        {
            if (origin == Origin.FilmAffinity)
            {
                progress.FilmAffinitySkippedMovies.Remove(movieName);
                progress.FilmAffinityMovies.Add(movieName);
            }
            else
            {
                progress.SkippedMovies.Remove(movieName);
                progress.Movies.Add(movieName);
            }
        }

        SaveProgress(progress);
    }

    public void MovieSkipped(Origin origin, string movieName, bool rewatch)
    {
        var progress = GetProgress() ?? new Progress();
        if (rewatch)
        {
            progress.SkippedMovieRewatchs.Add(movieName);
        }
        else
        {
            if (origin == Origin.FilmAffinity)
                progress.FilmAffinitySkippedMovies.Add(movieName);
            else
                progress.SkippedMovies.Add(movieName);
        }

        SaveProgress(progress);
    }

    private Progress? GetProgress()
    {
        try
        {
            if (!File.Exists(_path)) return new Progress();

            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<Progress>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while getting progress: {ex.Message}");
            return null;
        }
    }

    private void SaveProgress(Progress progress)
    {
        try
        {
            var json = JsonSerializer.Serialize(progress);
            File.WriteAllText(_path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while saving progress: {ex.Message}");
        }
    }

    private class Progress
    {
        public List<string> Shows { get; set; } = new();
        public List<string> ShowRewatchs { get; set; } = new();
        public List<string> SkippedShows { get; set; } = new();
        public List<string> SkippedShowRewatchs { get; set; } = new();
        public List<string> Movies { get; set; } = new();
        public List<string> MovieRewatchs { get; set; } = new();
        public List<string> SkippedMovies { get; set; } = new();
        public List<string> SkippedMovieRewatchs { get; set; } = new();
        public List<string> FilmAffinityMovies { get; set; } = new();
        public List<string> FilmAffinitySkippedMovies { get; set; } = new();
    }
}