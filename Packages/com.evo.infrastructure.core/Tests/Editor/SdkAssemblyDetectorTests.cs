using Evo.Infrastructure.Core.Editor.Setup;
using NUnit.Framework;

namespace Evo.Infrastructure.Core.Editor.Tests
{
    public sealed class SdkAssemblyDetectorTests
    {
        [TestCase("Assets/Firebase/Plugins/Firebase.Analytics.dll")]
        [TestCase("Assets/Firebase/Plugins/firebase.analytics.DLL")]
        public void FirebasePrecompiledDll_IsRecognized(string path)
        {
            Assert.That(
                SdkAssemblyDetector.MatchesPrecompiledAssemblyPath(path, "Firebase.Analytics"),
                Is.True);
        }

        [TestCase("Assets/Firebase/Plugins/Firebase.App.dll")]
        [TestCase("Assets/Firebase/Plugins/Firebase.Analytics.pdb")]
        [TestCase("")]
        public void UnrelatedAsset_IsRejected(string path)
        {
            Assert.That(
                SdkAssemblyDetector.MatchesPrecompiledAssemblyPath(path, "Firebase.Analytics"),
                Is.False);
        }
    }
}
