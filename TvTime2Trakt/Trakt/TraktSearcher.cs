using TraktNet.Enums;
using TraktNet;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Shows;
using System.Text.RegularExpressions;

namespace TvTime2Trakt.Trakt;

public static class TraktSearcher
{
    public static async Task<List<ITraktShow>> GetShowsByNameAsync(TraktClient traktClient, string showName)
    {
        var shows = new List<ITraktShow>();

        var searchResults = await traktClient.Search.GetTextQueryResultsAsync(TraktSearchResultType.Show, showName);
        if (searchResults is null || !searchResults.Any()) return shows;

        int? year = null;
        const string yearPattern = @"\((\d{4})\)"; // Captures the year inside the brackets
        var match = Regex.Match(showName, yearPattern);
        if (match.Success) year = int.Parse(match.Groups[1].Value);

        // Remove all text inside brackets
        var parsedShowName = Regex.Replace(showName, @"\s*\([^)]*\)", "").Trim();

        foreach (var searchResult in searchResults)
            if (searchResult.Show != null)
            {
                if ((year.HasValue && searchResult.Show.Year.HasValue && searchResult.Show.Year != year) ||
                    !searchResult.Show.Title.Contains(parsedShowName))
                    continue;

                shows.Add(searchResult.Show);
            }

        return shows;
    }

    public static async Task<ITraktShow?> GetShowByTraktIdAsync(TraktClient traktClient, string? showId)
    {
        if (showId is null) return null;

        var show = await traktClient.Shows.GetShowAsync(showId);
        return show.Value;
    }

    public static ITraktShow? FilterWithManualInput(List<ITraktShow> shows)
    {
        ITraktShow? show = null;
        Console.WriteLine("Please select an option:");
        Console.WriteLine("0. None");
        for (var i = 0; i < shows.Count; i++)
            Console.WriteLine(
                $"{i + 1}. {shows[i].Title} | https://trakt.tv/shows/{shows[i].Ids.Trakt}");

        Console.Write("Enter the number of your choice (default 1): ");
        var invalidSelection = true;
        while (invalidSelection)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                show = shows[0];
                invalidSelection = false;
            }
            else
            {
                if (int.TryParse(input, out var selection))
                {
                    if (selection > 0 && selection <= shows.Count)
                    {
                        show = shows[selection - 1];
                        invalidSelection = false;
                    }
                    else if (selection == 0)
                    {
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid selection. Please select a number from the list.");
                }
            }
        }

        return show;
    }

    public static async Task<List<ITraktMovie>> GetMoviesByNameAsync(TraktClient traktClient, string movieName,
        int? year)
    {
        var movies = new List<ITraktMovie>();

        var searchResults = await traktClient.Search.GetTextQueryResultsAsync(TraktSearchResultType.Movie, movieName);
        if (searchResults is null || !searchResults.Any()) return movies;

        // Remove all text inside brackets
        var parsedMovieName = Regex.Replace(movieName, @"\s*\([^)]*\)", "").Trim();

        foreach (var searchResult in searchResults)
            if (searchResult.Movie != null)
            {
                if (year.HasValue)
                {
                    if (!searchResult.Movie.Year.HasValue) continue;

                    if (searchResult.Movie.Year != year) continue;
                }

                if (!searchResult.Movie.Title.ToLower().Contains(parsedMovieName.ToLower())) continue;

                if (!parsedMovieName.Contains("making") &&
                    searchResult.Movie.Title.ToLower().Contains("making"))
                    continue;

                if (!parsedMovieName.Contains("behind") &&
                    searchResult.Movie.Title.ToLower().Contains("behind"))
                    continue;

                movies.Add(searchResult.Movie);
            }

        return movies;
    }

    public static async Task<ITraktMovie?> GetMovieByTraktIdAsync(TraktClient client, string? manualMovieId)
    {
        if (manualMovieId is null) return null;

        var movie = await client.Movies.GetMovieAsync(manualMovieId);
        return movie.Value;
    }

    public static ITraktMovie? FilterWithManualInput(List<ITraktMovie> movies)
    {
        ITraktMovie? movie = null;
        Console.WriteLine("Please select an option:");
        Console.WriteLine("0. None");
        for (var i = 0; i < movies.Count; i++)
            Console.WriteLine(
                $"{i + 1}. {movies[i].Title} [{movies[i].Year}] | https://trakt.tv/movies/{movies[i].Ids.Trakt}");

        Console.Write("Enter the number of your choice (default 1): ");
        var invalidSelection = true;

        while (invalidSelection)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                movie = movies[0];
                invalidSelection = false;
            }
            else
            {
                if (int.TryParse(input, out var selection))
                {
                    if (selection > 0 && selection <= movies.Count)
                    {
                        movie = movies[selection - 1];
                        invalidSelection = false;
                    }
                    else if (selection == 0)
                    {
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid selection. Please select a number from the list.");
                }
            }
        }

        return movie;
    }
}