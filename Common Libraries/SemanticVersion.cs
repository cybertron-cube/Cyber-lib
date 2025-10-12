using System.Text;

namespace Cybertron;

/// <summary>
/// Represents a semantic version number
/// </summary>
public class SemanticVersion : IVersion
{
    /// <summary>
    /// The major part of the version representing breaking changes
    /// </summary>
    public int Major => _major;
    
    /// <summary>
    /// The minor part of the version representing feature additions
    /// </summary>
    public int Minor => _minor;
    
    /// <summary>
    /// The patch part of the version representing bug fixes
    /// </summary>
    public int Patch => _patch;
    
    /// <summary>
    /// The identifier part of the pre-release version usually representing a branch/tag name like "beta"
    /// </summary>
    public string? Identifier => _identifier;
    
    /// <summary>
    /// The build part of the pre-release version usually incremented on every commit
    /// </summary>
    public int Build => _build;
    
    /// <summary>
    /// Whether this semantic version represents a pre-release
    /// </summary>
    public bool IsPreRelease => _identifier is not null;
    
    private readonly int _major;
    private readonly int _minor;
    private readonly int _patch;
    private readonly string? _identifier;
    private readonly int _build = -1;
    
    /// <summary>
    /// Instantiate a semantic version representing a release
    /// </summary>
    /// <param name="major">The major version number</param>
    /// <param name="minor">The minor version number</param>
    /// <param name="patch">The patch version number</param>
    /// <example>2.20.9 is equal to: new SemanticVersion(2, 20, 9)</example>
    public SemanticVersion(int major, int minor, int patch)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(major);
        ArgumentOutOfRangeException.ThrowIfNegative(minor);
        ArgumentOutOfRangeException.ThrowIfNegative(patch);
        
        _major = major;
        _minor = minor;
        _patch = patch;
        _build = -1;
    }
    
    /// <summary>
    /// Instantiate a semantic version representing a pre-release
    /// </summary>
    /// <param name="major">The major version number</param>
    /// <param name="minor">The minor version number</param>
    /// <param name="patch">The patch version number</param>
    /// <param name="identifier">The static part of the identifier</param>
    /// <param name="build">The dynamic part of the identifier</param>
    /// <example>2.0.0-beta.2 is equal to: new SemanticVersion(2, 0, 0, "beta", 2)</example>
    public SemanticVersion(int major, int minor, int patch, string identifier, int build)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(major);
        ArgumentOutOfRangeException.ThrowIfNegative(minor);
        ArgumentOutOfRangeException.ThrowIfNegative(patch);
        ArgumentOutOfRangeException.ThrowIfNegative(build);
        
        _major = major;
        _minor = minor;
        _patch = patch;
        _identifier = identifier;
        _build = build;
    }
    
    /// <summary>
    /// Instantiate a semantic version
    /// </summary>
    /// <param name="version">
    /// <para>String representing a semantic version</para>
    /// <para>major.minor.patch if normal release</para>
    /// <para>major.minor.patch-identifier.build if pre-release</para>
    /// </param>
    /// <exception cref="ArgumentException">Version string is not in specified format</exception>
    public SemanticVersion(string version)
    {
        if (!char.IsDigit(version[0]))
            throw new ArgumentException("Version string is not in proper format", nameof(version));
        
        var periodCount = 0;
        
        var major = false;
        var minor = false;
        var patch = false;
        var identifier = false;
        
        var sb = new StringBuilder(10);
        
        foreach (char c in version)
        {
            if (c == '.')
            {
                periodCount++;
                if (periodCount > 3)
                    throw new ArgumentException("Version string is not in proper format", nameof(version));

                if (!major)
                {
                    _major = Convert.ToInt32(sb.ToString());
                    sb.Clear();
                    major = true;
                }
                else if (!minor)
                {
                    _minor = Convert.ToInt32(sb.ToString());
                    sb.Clear();
                    minor = true;
                }
                else if (!identifier)
                {
                    _identifier = sb.ToString();
                    sb.Clear();
                    identifier = true;
                }
                else throw new ArgumentException("Version string is not in proper format", nameof(version));
            }
            else if (c == '-')
            {
                if (!patch)
                {
                    _patch = Convert.ToInt32(sb.ToString());
                    sb.Clear();
                    patch = true;
                }
                else throw new ArgumentException("Version string is not in proper format", nameof(version));
            }
            else if (!char.IsDigit(c) && !patch)
            {
                throw new ArgumentException("Version string is not in proper format", nameof(version));
            }
            else
            {
                sb.Append(c);
            }
        }
        
        if (identifier)
        {
            _build = Convert.ToInt32(sb.ToString());
        }
        else if (minor)
        {
            _patch = Convert.ToInt32(sb.ToString());
            _identifier = null;
            _build = -1;
        }
        else throw new ArgumentException("Version string is not in proper format", nameof(version));
    }
    
    public SemanticVersion() { }
    
    public bool Equals(IVersion? other)
    {
        if (other is not SemanticVersion version)
            throw new ArgumentException("Must be of type SemanticVersion and not null", nameof(other));
        
        return ReferenceEquals(version, this) ||
                _major == version._major &&
                _minor == version._minor &&
                _identifier == version._identifier &&
                _build == version._build;
    }
    
    /// <summary>
    /// Compares the current Version object to a specified object and returns an indication of their relative values.
    /// </summary>
    /// <param name="version">An object to compare, or null</param>
    /// <returns>A signed integer that indicates the relative values of the two objects, as shown in the following table
    /// <para>
    /// <list type="table">
    /// <listheader>
    /// <term>Return Value</term>
    /// <description>Meaning</description>
    /// </listheader>
    /// <item>
    /// <term>Less than zero</term>
    /// <description>The current <see cref="SemanticVersion"/> object is a version before <paramref name="version"/></description>
    /// </item>
    /// <item>
    /// <term>Zero</term>
    /// <description>The current <see cref="SemanticVersion"/> object is the same version as <paramref name="version"/></description>
    /// </item>
    /// <item>
    /// <term>Greater than zero</term>
    /// <description>The current <see cref="SemanticVersion"/> object is a version after <paramref name="version"/></description>
    /// </item>
    /// </list>
    /// </para>
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="version"/> is not of type <see cref="SemanticVersion"/></exception>
    public int CompareTo(object? version)
    {
        return version switch
        {
            null => 1,
            SemanticVersion v => CompareTo(v),
            _ => throw new ArgumentException("Must be of type SemanticVersion", nameof(version))
        };
    }
    
    /// <summary>
    /// Compares the current <see cref="SemanticVersion"/> object to a specified <see cref="SemanticVersion"/> and returns an indication of their relative values.
    /// </summary>
    /// <param name="version">A <see cref="SemanticVersion"/> to compare, or null</param>
    /// <returns><inheritdoc cref="CompareTo(object?)"/></returns>
    public int CompareTo(SemanticVersion? version)
    {
        return
            ReferenceEquals(version, this) ? 0 :
            version is null ? 1 :
            _major != version._major ? (_major > version._major ? 1 : -1) :
            _minor != version._minor ? (_minor > version._minor ? 1 : -1) :
            _patch != version._patch ? (_patch > version._patch ? 1 : -1) :
            !IsPreRelease && version.IsPreRelease ? 1 :
            IsPreRelease && !version.IsPreRelease ? -1 :
            !string.Equals(_identifier, version._identifier, StringComparison.OrdinalIgnoreCase) ? string.Compare(_identifier, version._identifier, StringComparison.Ordinal) :
            _build != version._build ? (_build > version._build ? 1 : -1) :
            0;
    }
    
    public override string ToString()
    {
        return IsPreRelease ? $"{_major}.{_minor}.{_patch}-{_identifier}.{_build}"
            : $"{_major}.{_minor}.{_patch}";
    }
}