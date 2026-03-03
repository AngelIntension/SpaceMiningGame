using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VoidHarvest.Features.Docking.Data;

namespace VoidHarvest.Features.Docking.Tests
{
    [TestFixture]
    public class DockingConfigBlobTests
    {
        [Test]
        public void BlobFields_MatchSourceConfig()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<DockingConfigBlob>();
            root.MaxDockingRange = 500f;
            root.SnapRange = 30f;
            root.SnapDuration = 1.5f;
            root.UndockClearanceDistance = 100f;
            root.UndockDuration = 2f;
            root.ApproachTimeout = 120f;
            root.AlignTimeout = 30f;
            root.AlignDotThreshold = 0.999f;
            root.AlignAngVelThreshold = 0.01f;

            var blobRef = builder.CreateBlobAssetReference<DockingConfigBlob>(Allocator.Temp);

            Assert.AreEqual(500f, blobRef.Value.MaxDockingRange);
            Assert.AreEqual(30f, blobRef.Value.SnapRange);
            Assert.AreEqual(1.5f, blobRef.Value.SnapDuration);
            Assert.AreEqual(100f, blobRef.Value.UndockClearanceDistance);
            Assert.AreEqual(2f, blobRef.Value.UndockDuration);
            Assert.AreEqual(120f, blobRef.Value.ApproachTimeout);
            Assert.AreEqual(30f, blobRef.Value.AlignTimeout);
            Assert.AreEqual(0.999f, blobRef.Value.AlignDotThreshold, 0.0001f);
            Assert.AreEqual(0.01f, blobRef.Value.AlignAngVelThreshold, 0.0001f);

            blobRef.Dispose();
        }

        [Test]
        public void BuildFromConfig_MapsAllFields()
        {
            var config = ScriptableObject.CreateInstance<DockingConfig>();
            config.MaxDockingRange = 450f;
            config.SnapRange = 25f;
            config.SnapDuration = 2.0f;
            config.UndockClearanceDistance = 80f;
            config.UndockDuration = 3f;
            config.ApproachTimeout = 90f;
            config.AlignTimeout = 20f;
            config.AlignDotThreshold = 0.998f;
            config.AlignAngVelThreshold = 0.02f;

            var blobRef = DockingConfigBlob.BuildFromConfig(config);

            Assert.AreEqual(450f, blobRef.Value.MaxDockingRange);
            Assert.AreEqual(25f, blobRef.Value.SnapRange);
            Assert.AreEqual(2.0f, blobRef.Value.SnapDuration);
            Assert.AreEqual(80f, blobRef.Value.UndockClearanceDistance);
            Assert.AreEqual(3f, blobRef.Value.UndockDuration);
            Assert.AreEqual(90f, blobRef.Value.ApproachTimeout);
            Assert.AreEqual(20f, blobRef.Value.AlignTimeout);
            Assert.AreEqual(0.998f, blobRef.Value.AlignDotThreshold, 0.0001f);
            Assert.AreEqual(0.02f, blobRef.Value.AlignAngVelThreshold, 0.0001f);

            blobRef.Dispose();
            Object.DestroyImmediate(config);
        }
    }
}
