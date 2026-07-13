using System.Collections.Generic;
using System.Reflection;
using Evo.Infrastructure.Core.Editor;
using NUnit.Framework;

namespace Evo.Infrastructure.Core.Tests.Editor
{
    public sealed class EvoUpdateVersionDetectionTests
    {
        [Test]
        public void GitHubTagsResponse_SelectsHighestSemanticVersion()
        {
            const string json = "[{\"name\":\"v0.5.9\"},{\"name\":\"v0.5.10\"},{\"name\":\"v0.4.12\"}]";

            var tagNames = Invoke<IReadOnlyList<string>>("ParseGitHubTagNames", json);
            var latestTag = Invoke<string>("SelectLatestGitHubTag", tagNames);

            Assert.That(latestTag, Is.EqualTo("v0.5.10"));
        }

        private static TResult Invoke<TResult>(string methodName, object argument)
        {
            var method = typeof(InfrastructureSetupWizardWindow).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' was not found.");
            return (TResult)method.Invoke(null, new[] { argument });
        }
    }
}
