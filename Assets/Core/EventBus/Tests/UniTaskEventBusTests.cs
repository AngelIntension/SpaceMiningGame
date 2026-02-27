using System.Threading;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using VoidHarvest.Core.EventBus;

namespace VoidHarvest.Core.EventBus.Tests
{
    [TestFixture]
    public class UniTaskEventBusTests
    {
        private UniTaskEventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _bus = new UniTaskEventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _bus.Dispose();
        }

        private readonly struct TestEvent
        {
            public readonly int Value;
            public TestEvent(int value) { Value = value; }
        }

        private readonly struct OtherEvent
        {
            public readonly string Message;
            public OtherEvent(string message) { Message = message; }
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new TestEvent(1)));
        }

        [Test]
        public void Subscribe_ReceivesPublishedEvent()
        {
            var cts = new CancellationTokenSource();
            int received = -1;

            // Start subscriber on the UniTask thread pool
            UniTask.Create(async () =>
            {
                await foreach (var evt in _bus.Subscribe<TestEvent>().WithCancellation(cts.Token))
                {
                    received = evt.Value;
                    cts.Cancel();
                }
            }).Forget();

            // Publish synchronously — Channel writer delivers immediately
            _bus.Publish(new TestEvent(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void MultipleSubscribers_EachReceivesEvent()
        {
            var cts = new CancellationTokenSource();
            int received1 = -1;
            int received2 = -1;

            UniTask.Create(async () =>
            {
                await foreach (var evt in _bus.Subscribe<TestEvent>().WithCancellation(cts.Token))
                {
                    received1 = evt.Value;
                    break;
                }
            }).Forget();

            UniTask.Create(async () =>
            {
                await foreach (var evt in _bus.Subscribe<TestEvent>().WithCancellation(cts.Token))
                {
                    received2 = evt.Value;
                    break;
                }
            }).Forget();

            _bus.Publish(new TestEvent(99));

            Assert.AreEqual(99, received1);
            Assert.AreEqual(99, received2);

            cts.Cancel();
        }

        [Test]
        public void DifferentEventTypes_AreIndependent()
        {
            bool testReceived = false;
            var cts = new CancellationTokenSource();

            UniTask.Create(async () =>
            {
                await foreach (var _ in _bus.Subscribe<TestEvent>().WithCancellation(cts.Token))
                {
                    testReceived = true;
                    break;
                }
            }).Forget();

            _bus.Publish(new OtherEvent("hello"));

            Assert.IsFalse(testReceived);

            cts.Cancel();
        }
    }
}
