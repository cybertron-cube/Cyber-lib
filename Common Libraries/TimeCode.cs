using System.Text.RegularExpressions;

namespace Cybertron;

/// <summary>
/// Represents a time code consisting of hours, minutes, seconds, milliseconds
/// </summary>
    
public partial class TimeCode
{
    [GeneratedRegex(@"^(?<hours>\d\d):(?<minutes>\d\d):(?<seconds>\d\d).(?<milliseconds>\d\d\d)$")]
    
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
            UpdateUnits(TimeCodeUnit.Minute);
        }
    }

    private int _totalSeconds;

    public int TotalSeconds
    {
        get => _totalSeconds;
        set
        {
            _totalSeconds = value;
            UpdateUnits(TimeCodeUnit.Second);
        }
    }
    
    private int _totalMilliseconds;

    public int TotalMilliseconds
    {
        get => _totalMilliseconds;
        set
        {
            _totalMinutes = value;
            UpdateUnits(TimeCodeUnit.Millisecond);
        }
    }

    private double _fps;

    public double Fps
    {
        get => _fps;
        set
        {
            _fps = value;
            UpdateFormattedString();
        }
    }

    public int Frame { get; private set; }

    private void CalculateFrameNumber()
    {
        // non-drop frame
        var frame = GetFrameNumber();

        if (StringFormat is TimeCodeFormat.SmpteNdf or TimeCodeFormat.Basic)
        {
            Frame = frame;
            return;
        }

        var totalDropFrames = _totalMinutes * 2 - _totalMinutes / 10;
        AddFrames(totalDropFrames);
        
        // https://en.wikipedia.org/wiki/SMPTE_timecode#Drop-frame_timecode
        frame = GetFrameNumber();
        if (frame is 0 or 1 && _minutes % 10 != 0 && _seconds == 0)
        {
            frame += 2;
        }

        Frame = frame;
    }
    
    private int GetFrameNumber() => (int)((double)_milliseconds / 1000 * _fps);

    private void AddFrames(int frames)
    {
        if (_fps <= 0)
            return;

        var seconds = frames / _fps;
        
        SetExactUnits(GetExactUnits(TimeCodeUnit.Second) + seconds, TimeCodeUnit.Second, false);
    }

    private TimeCodeFormat _stringFormat = TimeCodeFormat.Basic;

    public TimeCodeFormat StringFormat
    {
        get => _stringFormat;
        set
        {
            _stringFormat = value;
            UpdateFormattedString();
        }
    }
    
    private string _formattedString;
    
    /// <summary>
    /// Time code represented in a <see cref="TimeCodeFormat"/>, by default is <see cref="TimeCodeFormat.Basic"/>
    /// </summary>
    public string FormattedString => _formattedString;

    public TimeCode(int hours, int minutes, int seconds, int milliseconds)
    {
        _hours = hours;
        _minutes = minutes;
        _seconds = seconds;
        _milliseconds = milliseconds;
        
        UpdateFormattedString();

        _totalMinutes = hours * 60 + _minutes;
        _totalSeconds = hours * 3600
                        + minutes * 60
                        + seconds;
        _totalMilliseconds = hours * 3600000
                             + minutes * 60000
                             + seconds * 1000
                             + milliseconds;
    }
    
    public TimeCode(int hours, int minutes, int seconds, int milliseconds, double fps)
    {
        _hours = hours;
        _minutes = minutes;
        _seconds = seconds;
        _milliseconds = milliseconds;

        _fps = fps;
        _stringFormat = fps - Math.Floor(fps) < 0.001 ? TimeCodeFormat.SmpteNdf : TimeCodeFormat.SmpteDf;
        
        UpdateFormattedString();

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
        
        UpdateFormattedString();
    }
    
    public double GetExactUnits(TimeCodeUnit timeCodeUnit) //TODO add bool total = false param
    {
        return timeCodeUnit switch
        {
            TimeCodeUnit.Hour => _hours
                             + (double)_minutes / 60
                             + (double)_seconds / 3600
                             + (double)_milliseconds / 3600000,
            TimeCodeUnit.Minute => _hours * 60
                               + _minutes
                               + (double)_seconds / 60
                               + (double)_milliseconds / 60000,
            TimeCodeUnit.Second => _hours * 3600
                               + _minutes * 60
                               + _seconds
                               + (double)_milliseconds / 1000,
            TimeCodeUnit.Millisecond => _hours * 3600000
                                    + _minutes * 60000
                                    + _seconds * 1000
                                    + _milliseconds,
            _ => throw new ArgumentOutOfRangeException(nameof(timeCodeUnit), timeCodeUnit, null)
        };
    }
    
    public static double GetExactUnits(TimeCodeUnit timeCodeUnit, string timeCode)
    {
        return timeCodeUnit switch
        {
            TimeCodeUnit.Hour => int.Parse(timeCode.Substring(0, 2))
                             + double.Parse(timeCode.Substring(3, 2)) / 60
                             + double.Parse(timeCode.Substring(6, 2)) / 3600
                             + double.Parse(timeCode.Substring(9, 3)) / 3600000,
            TimeCodeUnit.Minute => int.Parse(timeCode.Substring(0, 2)) * 60
                               + int.Parse(timeCode.Substring(3, 2))
                               + double.Parse(timeCode.Substring(6, 2)) / 60
                               + double.Parse(timeCode.Substring(9, 3)) / 60000,
            TimeCodeUnit.Second => int.Parse(timeCode.Substring(0, 2)) * 3600
                               + int.Parse(timeCode.Substring(3, 2)) * 60
                               + int.Parse(timeCode.Substring(6, 2))
                               + double.Parse(timeCode.Substring(9, 3)) / 1000,
            TimeCodeUnit.Millisecond => int.Parse(timeCode.Substring(0, 2)) * 3600000
                                    + int.Parse(timeCode.Substring(3, 2)) * 60000
                                    + int.Parse(timeCode.Substring(6, 2)) * 1000
                                    + int.Parse(timeCode.Substring(9, 3)),
            _ => throw new ArgumentOutOfRangeException(nameof(timeCodeUnit), timeCodeUnit, null)
        };
    }
    
    public void SetExactUnits(double time, TimeCodeUnit timeCodeUnit) //TODO add bool total = false param
    {
        switch (timeCodeUnit)
        {
            case TimeCodeUnit.Hour:
                throw new NotImplementedException();
            case TimeCodeUnit.Minute:
                throw new NotImplementedException();
            case TimeCodeUnit.Second:
                _totalSeconds = (int)time;
                _milliseconds = (int)((time % 1) * 1000);
                _totalMilliseconds = _totalSeconds * 1000 + _milliseconds;
                _totalMinutes = _totalSeconds / 60;
                
                _seconds = _totalSeconds % 60;
                _minutes = _totalMinutes % 60;
                _hours = _totalMinutes / 60;
                break;
            case TimeCodeUnit.Millisecond:
                throw new ArgumentOutOfRangeException(nameof(timeCodeUnit), timeCodeUnit, null);
        }
        
        UpdateFormattedString();
    }
    
    private void SetExactUnits(double time, TimeCodeUnit timeCodeUnit, bool updateFormatString = false)
    {
        switch (timeCodeUnit)
        {
            case TimeCodeUnit.Hour:
                throw new NotImplementedException();
            case TimeCodeUnit.Minute:
                throw new NotImplementedException();
            case TimeCodeUnit.Second:
                _totalSeconds = (int)time;
                _milliseconds = (int)((time % 1) * 1000);
                _totalMilliseconds = _totalSeconds * 1000 + _milliseconds;
                _totalMinutes = _totalSeconds / 60;
                
                _seconds = _totalSeconds % 60;
                _minutes = _totalMinutes % 60;
                _hours = _totalMinutes / 60;
                break;
            case TimeCodeUnit.Millisecond:
                throw new ArgumentOutOfRangeException(nameof(timeCodeUnit), timeCodeUnit, null);
        }
        
        if (updateFormatString)
            UpdateFormattedString();
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
        CalculateFrameNumber();
        _formattedString = StringFormat switch
        {
            TimeCodeFormat.Basic =>
                $"{PadTimeCodeUnit(_hours)}:{PadTimeCodeUnit(_minutes)}:{PadTimeCodeUnit(_seconds)}.{PadTimeCodeUnit(_milliseconds, 3)}",
            TimeCodeFormat.SmpteNdf =>
                $"{PadTimeCodeUnit(_hours)}:{PadTimeCodeUnit(_minutes)}:{PadTimeCodeUnit(_seconds)}:{PadTimeCodeUnit(Frame)}",
            TimeCodeFormat.SmpteDf =>
                $"{PadTimeCodeUnit(_hours)}:{PadTimeCodeUnit(_minutes)}:{PadTimeCodeUnit(_seconds)};{PadTimeCodeUnit(Frame)}",
            _ => string.Empty
        };
    }

    private void UpdateUnits(TimeCodeUnit timeCodeUnit)
    {
        switch (timeCodeUnit)
        {
            case TimeCodeUnit.Minute:
                _totalSeconds = _totalMinutes * 60;
                _totalMilliseconds = _totalSeconds * 1000;
                
                _milliseconds = 0;
                break;
            case TimeCodeUnit.Second:
                _totalMilliseconds = _totalSeconds * 1000;
                _totalMinutes = _totalSeconds / 60;
                
                _milliseconds = 0;
                break;
            case TimeCodeUnit.Millisecond:
                _totalSeconds = _totalMilliseconds / 1000;
                _totalMinutes = _totalSeconds / 60;

                _milliseconds = _totalMilliseconds % 1000;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(timeCodeUnit), timeCodeUnit, null);
        }

        _seconds = _totalSeconds % 60;
        _minutes = _totalMinutes % 60;
        _hours = _totalMinutes / 60;
        
        UpdateFormattedString();
    }
}