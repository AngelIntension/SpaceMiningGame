using NUnit.Framework;
using VoidHarvest.Features.StationServices.Views;

namespace VoidHarvest.Features.StationServices.Tests
{
    /// <summary>
    /// Tests for refining job notification state tracking.
    /// See Spec 006 US6: Refining Job Notifications, FR-044.
    /// </summary>
    [TestFixture]
    public sealed class RefiningNotificationTests
    {
        [Test]
        public void InitialCount_IsZero()
        {
            var tracker = new RefiningNotificationTracker();
            Assert.AreEqual(0, tracker.PendingCount);
        }

        [Test]
        public void OnJobCompleted_IncrementsCount()
        {
            var tracker = new RefiningNotificationTracker();
            tracker.OnJobCompleted(1, "job-1");
            Assert.AreEqual(1, tracker.PendingCount);
        }

        [Test]
        public void OnJobCollected_DecrementsCount()
        {
            var tracker = new RefiningNotificationTracker();
            tracker.OnJobCompleted(1, "job-1");
            tracker.OnJobCollected(1, "job-1");
            Assert.AreEqual(0, tracker.PendingCount);
        }

        [Test]
        public void MultipleCompletions_AccumulateCount()
        {
            var tracker = new RefiningNotificationTracker();
            tracker.OnJobCompleted(1, "job-1");
            tracker.OnJobCompleted(1, "job-2");
            tracker.OnJobCompleted(2, "job-3");
            Assert.AreEqual(3, tracker.PendingCount);
        }

        [Test]
        public void CollectAll_ResetsToZero()
        {
            var tracker = new RefiningNotificationTracker();
            tracker.OnJobCompleted(1, "job-1");
            tracker.OnJobCompleted(1, "job-2");
            tracker.OnJobCollected(1, "job-1");
            tracker.OnJobCollected(1, "job-2");
            Assert.AreEqual(0, tracker.PendingCount);
        }

        [Test]
        public void HasPending_FalseWhenZero()
        {
            var tracker = new RefiningNotificationTracker();
            Assert.IsFalse(tracker.HasPending);
        }

        [Test]
        public void HasPending_TrueWhenPositive()
        {
            var tracker = new RefiningNotificationTracker();
            tracker.OnJobCompleted(1, "job-1");
            Assert.IsTrue(tracker.HasPending);
        }

        [Test]
        public void CollectUnknownJob_DoesNotGoNegative()
        {
            var tracker = new RefiningNotificationTracker();
            tracker.OnJobCollected(1, "unknown");
            Assert.AreEqual(0, tracker.PendingCount);
        }
    }
}
