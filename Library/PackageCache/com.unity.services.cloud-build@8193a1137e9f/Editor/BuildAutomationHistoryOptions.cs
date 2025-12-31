namespace Unity.Services.CloudBuild.Editor
{
    internal class BuildAutomationHistoryOptions
    {
        internal int PageNumber { get; set; } = 1;
        internal int PerPage { get; set; } = 10;
        internal bool OnlyEditorBuilds { get; set; } = true;
        internal bool OnlyMyBuilds { get; set; } = false;
        internal bool ShowDeleted { get; set; }
        internal string[] StatusFilter { get; set; }
        internal string[] PlatformFilter { get; set; }
        internal string[] ProfileFilter { get; set; }
        internal string Search { get; set; }

        internal string ToQueryParams()
        {
            var statusQuery = StatusFilter != null && StatusFilter.Length > 0 ? $"&buildStatus={string.Join(",", StatusFilter)}" : "";
            var platformQuery = PlatformFilter != null && PlatformFilter.Length > 0 ? $"&platform={string.Join(",", PlatformFilter)}" : "";
            var profileQuery = ProfileFilter != null && ProfileFilter.Length > 0 ? $"&buildProfile={string.Join(",", ProfileFilter)}" : "";
            var searchQuery = string.IsNullOrEmpty(Search) ? "" : $"&search={Search}";

            return $"page={PageNumber}&per_page={PerPage}&onlyEditorBuilds={OnlyEditorBuilds.ToLowerString()}" +
                   $"&onlyMyBuilds={OnlyMyBuilds.ToLowerString()}&showDeleted={ShowDeleted.ToLowerString()}" +
                   $"{statusQuery}{platformQuery}{profileQuery}{searchQuery}";
        }
    }
}

internal static class Extensions
{
    internal static string ToLowerString(this bool _bool)
    {
        return _bool.ToString().ToLower();
    }
}
