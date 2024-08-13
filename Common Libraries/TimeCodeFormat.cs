namespace Cybertron;

/// <summary>
/// Available string formats for <see cref="TimeCode.StringFormat"/> property that changes
/// <see cref="TimeCode.FormattedString"/>
/// </summary>
public enum TimeCodeFormat
{
    /// <summary>
    /// HH:MM:SS:MsMsMs
    /// </summary>
    Basic,
        
    /// <summary>
    /// SMPTE non-drop-frame timecode, HH:MM:SS:Frame, requires fps property to be set accordingly as well as
    /// milliseconds in order to see a frame number different from zero
    /// </summary>
    SmpteNdf,
    
    /// <summary>
    /// SMPTE drop-frame timecode, HH:MM:SS;Frame, requires fps property to be set accordingly as well as milliseconds
    /// in order to see a frame number different from zero
    /// </summary>
    SmpteDf
}
