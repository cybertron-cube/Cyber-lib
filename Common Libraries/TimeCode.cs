using System.Text.RegularExpressions;

namespace Cybertron;

/// <summary>
/// Represents a time code consisting of hours, minutes, seconds, milliseconds
/// </summary>
    
//TODO remove Value, remove toDouble/ToInt/etc., add totalhours, totalminutes, totalseconds
public partial class TimeCode
{
    public enum TimeUnit
    {
        Millisecond,
        Second,
        Minute,
        Hour
    }

    [GeneratedRegex("^(?<hours>\\d\\d):(?<minutes>\\d\\d):(?<seconds>\\d\\d).(?<milliseconds>\\d\\d\\d)$")]
    
    private static partial Regex GenTimeCodeRegex();
    
    public static readonly Regex TimeCodeRegex = GenTimeCodeRegex();
    
    private int _hours;
    
    /// <summary>
    /// Hours unit of the time code
    /// </summary>
    public int Hours
    {
        get => _hours;
        set
        {
            if (value >= 0 && value < 100)
            {
                _hours = value;
                UpdateFormattedString();
            }
            else throw new ArgumentOutOfRangeException(nameof(value), value.ToString());
        }
    }
    private int _minutes;
    
    /// <summary>
    /// Minutes unit of the time code
    /// </summary>
    public int Minutes
    {
        get => _minutes;
        set
        {
            if (value >= 0 && value < 60)
            {
                _minutes = value;
                UpdateFormattedString();
            }
            else throw new ArgumentOutOfRangeException(nameof(value), value.ToString());
        }
    }
    private int _seconds;
    
    /// <summary>
    /// Seconds unit of the time code
    /// </summary>
    public int Seconds
    {
        get => _seconds;
        set
        {
            if (value >= 0 && value < 60)
            {
                _seconds = value;
                UpdateFormattedString();
            }
            else throw new ArgumentOutOfRangeException(nameof(value), value.ToString());
        }
    }
    private int _milliseconds;
    
    /// <summary>
    /// Milliseconds unit of the time code
    /// </summary>
    public int Milliseconds
    {
        get => _milliseconds;
        set
        {
            if (value >= 0 && value < 1000)
            {
                _milliseconds = value;
                UpdateFormattedString();
            }
            else throw new ArgumentOutOfRangeException(nameof(value), value.ToString());
        }
    }
    
    private int _totalMinutes;

    public int TotalMinutes
    {
        get => _totalMinutes;
        set
        {
            _totalMinutes = value;
            UpdateUnits(TimeUnit.Minute);
        }
    }

    private int _totalSeconds;

    public int TotalSeconds
    {
        get => _totalSeconds;
        set
        {
            _totalSeconds = value;
            UpdateUnits(TimeUnit.Second);
        }
    }
    
    private int _totalMilliseconds;

    public int TotalMilliseconds
    {
        get => _totalMilliseconds;
        set
        {
            _totalMinutes = value;
            UpdateUnits(TimeUnit.Millisecond);
        }
    }
    
    private string _formattedString;
    
    /// <summary>
    /// Time code represented in a properly formatted string (HH:MM:SS:MsMsMs)
    /// </summary>
    public string FormattedString => _formattedString;

    public TimeCode(int hours, int minutes, int seconds, int milliseconds)
    {
        _hours = hours;
        _minutes = minutes;
        _seconds = seconds;
        _milliseconds = milliseconds;
        
        _formattedString =
            $"{PadTimeCodeUnit(hours)}:{PadTimeCodeUnit(minutes)}:{PadTimeCodeUnit(seconds)}.{PadTimeCodeUnit(milliseconds, 3)}";

        _totalMinutes = hours * 60 + _minutes;
        _totalSeconds = hours * 3600
                        + minutes * 60
                        + seconds;
        _totalMilliseconds = hours * 3600000
                             + minutes * 60000
                             + seconds * 1000
                             + milliseconds;
    }

    public TimeCode(int totalSeconds)
    {
        _totalSeconds = totalSeconds;
        _totalMilliseconds = totalSeconds * 1000;
        _totalMinutes = totalSeconds / 60;
        //_totalHours = _totalMinutes / 60;

        _milliseconds = 0;
        _seconds = totalSeconds % 60;
        _minutes = _totalMinutes % 60;
        _hours = _totalMinutes / 60;
        
        _formattedString =
            $"{PadTimeCodeUnit(_hours)}:{PadTimeCodeUnit(_minutes)}:{PadTimeCodeUnit(_seconds)}.{PadTimeCodeUnit(_milliseconds, 3)}";
    }
    
    public double GetExactUnits(TimeUnit timeUnit) //TODO add bool total = false param
    {
        return timeUnit switch
        {
            TimeUnit.Hour => _hours
                             + (double)_minutes / 60
                             + (double)_seconds / 3600
                             + (double)_milliseconds / 3600000,
            TimeUnit.Minute => _hours * 60
                               + _minutes
                               + (double)_seconds / 60
                               + (double)_milliseconds / 60000,
            TimeUnit.Second => _hours * 3600
                               + _minutes * 60
                               + _seconds
                               + (double)_milliseconds / 1000,
            TimeUnit.Millisecond => _hours * 3600000
                                    + _minutes * 60000
                                    + _seconds * 1000
                                    + _milliseconds,
            _ => throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null)
        };
    }
    
    public static double GetExactUnits(TimeUnit timeUnit, string timeCode)
    {
        return timeUnit switch
        {
            TimeUnit.Hour => int.Parse(timeCode.Substring(0, 2))
                             + double.Parse(timeCode.Substring(3, 2)) / 60
                             + double.Parse(timeCode.Substring(6, 2)) / 3600
                             + double.Parse(timeCode.Substring(9, 3)) / 3600000,
            TimeUnit.Minute => int.Parse(timeCode.Substring(0, 2)) * 60
                               + int.Parse(timeCode.Substring(3, 2))
                               + double.Parse(timeCode.Substring(6, 2)) / 60
                               + double.Parse(timeCode.Substring(9, 3)) / 60000,
            TimeUnit.Second => int.Parse(timeCode.Substring(0, 2)) * 3600
                               + int.Parse(timeCode.Substring(3, 2)) * 60
                               + int.Parse(timeCode.Substring(6, 2))
                               + double.Parse(timeCode.Substring(9, 3)) / 1000,
            TimeUnit.Millisecond => int.Parse(timeCode.Substring(0, 2)) * 3600000
                                    + int.Parse(timeCode.Substring(3, 2)) * 60000
                                    + int.Parse(timeCode.Substring(6, 2)) * 1000
                                    + int.Parse(timeCode.Substring(9, 3)),
            _ => throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null)
        };
    }
    
    public void SetExactUnits(double time, TimeUnit timeUnit) //TODO add bool total = false param
    {
        switch (timeUnit)
        {
            case TimeUnit.Hour:
                throw new NotImplementedException();
            case TimeUnit.Minute:
                throw new NotImplementedException();
            case TimeUnit.Second:
                _totalSeconds = (int)time;
                _milliseconds = (int)((time % 1) * 1000);
                _totalMilliseconds = _totalSeconds * 1000 + _milliseconds;
                _totalMinutes = _totalSeconds / 60;
                
                _seconds = _totalSeconds % 60;
                _minutes = _totalMinutes % 60;
                _hours = _totalMinutes / 60;
                break;
            case TimeUnit.Millisecond:
                throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null);
        }
        
        _formattedString =
            $"{PadTimeCodeUnit(_hours)}:{PadTimeCodeUnit(_minutes)}:{PadTimeCodeUnit(_seconds)}.{PadTimeCodeUnit(_milliseconds, 3)}";
    }
    
    /// <summary>
    /// Creates a new <see cref="TimeCode"/> object from a string
    /// </summary>
    /// <param name="text">A string that contains a timecode represented in hh:mm:ss:msmsms</param>
    /// <returns>A <see cref="TimeCode"/> object from a valid string</returns>
    /// <exception cref="FormatException">Timecode string was not represented in hh:mm:ss:msmsms</exception>
    public static TimeCode Parse(string text)
    {
        Match match = TimeCodeRegex.Match(text);
        if (match.Success)
        {
            TimeCode timeCode = new(
                int.Parse(match.Groups["hours"].Value),
                int.Parse(match.Groups["minutes"].Value),
                int.Parse(match.Groups["seconds"].Value),
                int.Parse(match.Groups["milliseconds"].Value));
            return timeCode;
        }
        else throw new FormatException("Input string was not in a correct format.");
    }
    
    /// <summary>
    /// Creates a new <see cref="TimeCode"/> object from a string/>
    /// </summary>
    /// <param name="text">A string that contains a time code represented in hh:mm:ss:msmsms</param>
    /// <param name="timeCode">The <see cref="TimeCode"/> representation of the text</param>
    /// <returns>A boolean indicating whether the string is able to be parsed</returns>
    public static bool TryParse(string text, out TimeCode? timeCode)
    {
        Match match = TimeCodeRegex.Match(text);
        if (match.Success)
        {
            timeCode = new(
                int.Parse(match.Groups["hours"].Value),
                int.Parse(match.Groups["minutes"].Value),
                int.Parse(match.Groups["seconds"].Value),
                int.Parse(match.Groups["milliseconds"].Value));
            return true;
        }
        else
        {
            timeCode = null;
            return false;
        }
    }
    
    public static string PadTimeCodeUnit(int unit, int places = 2)
    {
        return unit.ToString().PadLeft(places, '0');
    }
    
    private void UpdateFormattedString()
    {
        _formattedString =
            $"{PadTimeCodeUnit(_hours)}:{PadTimeCodeUnit(_minutes)}:{PadTimeCodeUnit(_seconds)}.{PadTimeCodeUnit(_milliseconds, 3)}";
    }

    private void UpdateUnits(TimeUnit timeUnit)
    {
        switch (timeUnit)
        {
            case TimeUnit.Minute:
                _totalSeconds = _totalMinutes * 60;
                _totalMilliseconds = _totalSeconds * 1000;
                
                _milliseconds = 0;
                break;
            case TimeUnit.Second:
                _totalMilliseconds = _totalSeconds * 1000;
                _totalMinutes = _totalSeconds / 60;
                
                _milliseconds = 0;
                break;
            case TimeUnit.Millisecond:
                _totalSeconds = _totalMilliseconds / 1000;
                _totalMinutes = _totalSeconds / 60;

                _milliseconds = _totalMilliseconds % 1000;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null);
        }

        _seconds = _totalSeconds % 60;
        _minutes = _totalMinutes % 60;
        _hours = _totalMinutes / 60;
        
        _formattedString =
            $"{PadTimeCodeUnit(_hours)}:{PadTimeCodeUnit(_minutes)}:{PadTimeCodeUnit(_seconds)}.{PadTimeCodeUnit(_milliseconds, 3)}";
    }
}