using NUnit.Framework;

namespace Unity.Entities.Editor.Tests
{
    [TestFixture]
    class EntityBakingPreviewTests
    {
        [Test]
        public void EntityConversionPreview_ShowOnlyLiveAndConversionWorlds()
        {
            using (WorldScope.CaptureAndResetExistingWorlds())
            {
                using (var live = new World("live", WorldFlags.Live))
                using (var conversion = new World("conversion", WorldFlags.Conversion))
                using (var shadow = new World("shadow", WorldFlags.Shadow))
                using (var staging = new World("staging", WorldFlags.Staging))
                using (var streaming = new World("streaming", WorldFlags.Streaming))
                {
                    Assert.That(EntityBakingPreview.Worlds.FilteredWorlds, Does.Contain(live));
                    Assert.That(EntityBakingPreview.Worlds.FilteredWorlds, Does.Contain(conversion));

                    Assert.That(EntityBakingPreview.Worlds.FilteredWorlds, Does.Not.Contains(shadow));
                    Assert.That(EntityBakingPreview.Worlds.FilteredWorlds, Does.Not.Contains(staging));
                    Assert.That(EntityBakingPreview.Worlds.FilteredWorlds, Does.Not.Contains(streaming));
                }
            }
        }

        [Test]
        public void EntityConversionPreview_ShowCreatedWorlds()
        {
            using (WorldScope.CaptureAndResetExistingWorlds())
            {
                Assert.That(EntityBakingPreview.Worlds.FilteredWorlds, Is.Empty);

                using (new World("test", WorldFlags.Live))
                {
                    Assert.That(EntityBakingPreview.Worlds.FilteredWorlds, Is.Not.Empty);
                }
            }
        }
    }
}
