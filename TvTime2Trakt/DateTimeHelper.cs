namespace TvTime2Trakt;

public static class DateTimeHelper
{
    public static DateTime ConvertTvTimeDate(string tvTimeDate)
    {
        var unixTimestamp = ConvertCustomDateFormat(tvTimeDate);
        return ConvertFromUnixTimestamp(unixTimestamp / 1000000);
    }

    private static long ConvertCustomDateFormat(string customDate)
    {
        const int epochYear = 1970;

        var parts = customDate.Split(' ');
        var dateParts = parts[0].Split('-');
        var timeParts = parts[1].Split(':');

        var year = int.Parse(dateParts[0]);
        var month = int.Parse(dateParts[1]);
        var day = int.Parse(dateParts[2]);

        var hour = int.Parse(timeParts[0]);
        var minute = int.Parse(timeParts[1]);
        var second = int.Parse(timeParts[2]);

        var totalDays = CalculateDaysSinceEpoch(year, month, day, epochYear);
        long totalSecondsInDay = hour * 3600 + minute * 60 + second;
        var unixTimestamp = totalDays * 86400 + totalSecondsInDay;

        return unixTimestamp;
    }

    private static DateTime ConvertFromUnixTimestamp(long unixTimestamp)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        return dateTimeOffset.UtcDateTime;
    }

    private static long CalculateDaysSinceEpoch(int year, int month, int day, int epochYear)
    {
        long days = 0;

        for (var y = epochYear; y < year; y++)
        {
            days += IsLeapYear(y) ? 366 : 365;
        }

        for (var m = 1; m < month; m++)
        {
            days += DaysInMonth(year, m);
        }

        days += day - 1;

        return days;
    }

    private static int DaysInMonth(int year, int month)
    {
        switch (month)
        {
            case 2:
                return IsLeapYear(year) ? 29 : 28;
            case 4:
            case 6:
            case 9:
            case 11:
                return 30;
            default:
                return 31;
        }
    }

    private static bool IsLeapYear(int year)
    {
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }
}