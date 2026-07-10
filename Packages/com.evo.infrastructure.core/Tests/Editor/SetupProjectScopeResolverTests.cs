using Evo.Infrastructure.Core.Editor.Setup;
using NUnit.Framework;

namespace Evo.Infrastructure.Core.Editor.Tests
{
    public sealed class SetupProjectScopeResolverTests
    {
        [TestCase("Game.Runtime.EntryPoint.RuntimeProjectLifetimeScope")]
        [TestCase("Game.Runtime.Bootstrap.RuntimeProjectLifetimeScope")]
        [TestCase("RuntimeProjectLifetimeScope")]
        public void SupportedScopeNames_AreAccepted(string fullName)
        {
            Assert.That(SetupProjectScopeResolver.IsSupportedName(fullName), Is.True);
        }

        [TestCase("_Project.Scripts.Application.ProjectLifetimeScope")]
        [TestCase("")]
        [TestCase(null)]
        public void UnrelatedScopeNames_AreRejected(string fullName)
        {
            Assert.That(SetupProjectScopeResolver.IsSupportedName(fullName), Is.False);
        }
    }
}
