using Evo.Infrastructure.Editor.EvoTools.Build;
using NUnit.Framework;

namespace Evo.Infrastructure.Runtime.Editor.Tests
{
    public sealed class EvoBuildCiTagTests
    {
        [Test]
        public void TryParse_ValidTagWithoutDebugInfo_ReturnsParts()
        {
            var parsed = EvoBuildCiTag.TryParse("google_release_aab_8.5.17_123", out var tag, out var error);

            Assert.That(parsed, Is.True, error);
            Assert.That(tag.Platform, Is.EqualTo("google"));
            Assert.That(tag.BuildType, Is.EqualTo("release"));
            Assert.That(tag.ArtifactType, Is.EqualTo("aab"));
            Assert.That(tag.Version, Is.EqualTo("8.5.17"));
            Assert.That(tag.BuildNumber, Is.EqualTo(123));
            Assert.That(tag.DebugInfo, Is.Empty);
        }

        [Test]
        public void TryParse_ValidTagWithDebugInfo_ReturnsDebugInfo()
        {
            var parsed = EvoBuildCiTag.TryParse("google_develop_apk_8.5.17_124_debug", out var tag, out var error);

            Assert.That(parsed, Is.True, error);
            Assert.That(tag.BuildType, Is.EqualTo("develop"));
            Assert.That(tag.ArtifactType, Is.EqualTo("apk"));
            Assert.That(tag.BuildNumber, Is.EqualTo(124));
            Assert.That(tag.DebugInfo, Is.EqualTo("debug"));
        }

        [TestCase("")]
        [TestCase("google_release_aab_8.5.17")]
        [TestCase("google_release_aab_version_123")]
        [TestCase("google_release_aab_8.5.17_0")]
        [TestCase("google_release_aab_8.5.17_abc")]
        [TestCase("google_release_aab_8.5.17_123_debug_extra")]
        public void TryParse_InvalidTag_ReturnsError(string value)
        {
            var parsed = EvoBuildCiTag.TryParse(value, out _, out var error);

            Assert.That(parsed, Is.False);
            Assert.That(error, Is.Not.Empty);
        }
    }
}
