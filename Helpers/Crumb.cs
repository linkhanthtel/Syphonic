namespace Syphonic.Helpers;

/// <summary>
/// One step in a breadcrumb trail or a "where to next" link.
/// A step without a URL is the page the learner is on now.
/// </summary>
public sealed record Crumb(string Label, string? Url = null);
