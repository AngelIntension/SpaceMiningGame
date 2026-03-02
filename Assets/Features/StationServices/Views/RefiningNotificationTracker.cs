using System.Collections.Generic;

namespace VoidHarvest.Features.StationServices.Views
{
    /// <summary>
    /// Pure state tracker for pending completed refining jobs.
    /// Not a MonoBehaviour — used by HUD indicator for count tracking.
    /// See Spec 006 US6: Refining Job Notifications, FR-044.
    /// </summary>
    public sealed class RefiningNotificationTracker
    {
        private readonly HashSet<string> _pendingJobs = new HashSet<string>();

        public int PendingCount => _pendingJobs.Count;
        public bool HasPending => _pendingJobs.Count > 0;

        public void OnJobCompleted(int stationId, string jobId)
        {
            var key = $"{stationId}:{jobId}";
            _pendingJobs.Add(key);
        }

        public void OnJobCollected(int stationId, string jobId)
        {
            var key = $"{stationId}:{jobId}";
            _pendingJobs.Remove(key);
        }

        public void Clear()
        {
            _pendingJobs.Clear();
        }
    }
}
