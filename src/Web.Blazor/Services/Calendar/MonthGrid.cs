namespace SafetyScale.Web.Blazor.Services.Calendar;

/// <summary>Calendar grid helpers. Parity with React <c>monthGrid.ts</c>.</summary>
public static class MonthGrid
{
    public static readonly string[] WeekdayLabelsSmtwtfs = ["S", "M", "T", "W", "T", "F", "S"];

    public static string TodayKeyLocal()
    {
        var today = DateTime.Now;
        return DateKeyFromLocal(today.Year, today.Month, today.Day);
    }

    public static string DateKeyFromLocal(int year, int month, int day) =>
        $"{year:D4}-{month:D2}-{day:D2}";

    public static IReadOnlyList<MonthGridCell> BuildMonthGrid(int viewYear, int viewMonth0)
    {
        var first = new DateTime(viewYear, viewMonth0 + 1, 1);
        var startPad = (int)first.DayOfWeek;
        var daysInMonth = DateTime.DaysInMonth(viewYear, viewMonth0 + 1);
        var cells = new List<MonthGridCell>();

        var prevLast = DateTime.DaysInMonth(
            viewMonth0 == 0 ? viewYear - 1 : viewYear,
            viewMonth0 == 0 ? 12 : viewMonth0);
        for (var i = 0; i < startPad; i++)
        {
            var dayNum = prevLast - startPad + i + 1;
            var prevMonth0 = viewMonth0 == 0 ? 11 : viewMonth0 - 1;
            var prevYear = viewMonth0 == 0 ? viewYear - 1 : viewYear;
            var d = new DateTime(prevYear, prevMonth0 + 1, dayNum);
            cells.Add(new MonthGridCell(DateKeyFromLocal(d.Year, d.Month, d.Day), dayNum, false));
        }

        for (var day = 1; day <= daysInMonth; day++)
        {
            cells.Add(new MonthGridCell(
                DateKeyFromLocal(viewYear, viewMonth0 + 1, day),
                day,
                true));
        }

        var rem = cells.Count % 7;
        var tail = rem == 0 ? 0 : 7 - rem;
        var nextMonth0 = viewMonth0 == 11 ? 0 : viewMonth0 + 1;
        var nextYear = viewMonth0 == 11 ? viewYear + 1 : viewYear;
        for (var n = 1; n <= tail; n++)
        {
            var d = new DateTime(nextYear, nextMonth0 + 1, n);
            cells.Add(new MonthGridCell(DateKeyFromLocal(d.Year, d.Month, d.Day), n, false));
        }

        return cells;
    }
}

public sealed record MonthGridCell(string Key, int Label, bool InMonth);
