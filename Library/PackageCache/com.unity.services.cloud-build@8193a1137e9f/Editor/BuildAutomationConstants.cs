
namespace Unity.Services.CloudBuild.Editor
{
    internal static class BuildAutomationConstants
    {
        internal static readonly string[] FilterableBuildStatuses =
        {
            "queued",
            "sentToBuilder",
            "started",
            "success",
            "failure",
            "canceled",
        };

        internal static string GetFormattedStatusName(string status)
        {
            return status switch {
                "success" => "Success",
                "failure" => "Failure",
                "canceled" => "Canceled",
                "started" => "Building",
                "assignedToBuilder" => "Assigned To Builder",
                "sentToBuilder" => "Sent To Builder",
                "created" => "Queued",
                "queued" => "Queued",
                "restarted" => "Restarted",
                _ => "Unknown",
            };
        }

        internal static bool IsBuildPending(string status)
        {
            return status switch {
                "assignedToBuilder" => true,
                "sentToBuilder" => true,
                "created" => true,
                "queued" => true,
                _ => false,
            };
        }

        internal static bool IsBuildFinished(string status)
        {
            return status switch
            {
                "success" => true,
                "failure" => true,
                "canceled" => true,
                _ => false,
            };
        }

        internal static string GetStatusIcon(string status)
        {
            return status switch {
                "success" => "build_success",
                "failure" => "build_failed",
                "canceled" => "build_canceled",
                "started" => "build_started",
                "assignedToBuilder" => "build_sentToBuilder",
                "sentToBuilder" => "build_sentToBuilder",
                "created" => "build_queued",
                "queued" => "build_queued",
                "restarted" => "build_restarted",
                _ => "build_unknown",
            };
        }

        internal static bool IsBuildLogDownloadable(string status)
        {
            return status switch {
                "success" => true,
                "failure" => true,
                _ => false,
            };
        }
    }
}

