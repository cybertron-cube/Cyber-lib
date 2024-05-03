namespace Cybertron;

/// <summary>
/// When used with the updater class, requires a constructor with a single string argument that would represent a
/// version under your scheme. It would also be preferable to override the ToString() method as it will be used to
/// provide a user agent in the http request to github.
/// </summary>
public interface IVersion : IEquatable<IVersion>, IComparable;
