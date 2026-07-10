using System;
using System.IO;
using System.Linq;
using Evo.Infrastructure.Core.Editor.Setup.Scaffold;
using NUnit.Framework;

namespace Evo.Infrastructure.Core.Editor.Tests
{
    public sealed class ScaffoldPlanBuilderTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "EvoScaffoldPlanTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void MissingTemplate_IsConflict_EvenWhenTargetIsMissing()
        {
            var target = Path.Combine(_root, "RuntimeProjectLifetimeScope.cs");
            var template = Path.Combine(_root, "missing-template.txt");

            var plan = ScaffoldPlanBuilder.Build(
                new[] { (target, template) },
                Array.Empty<string>());

            Assert.That(plan.HasConflicts, Is.True);
            Assert.That(plan.Items.Single().Kind, Is.EqualTo(ScaffoldChangeKind.Conflict));
        }

        [Test]
        public void ExistingProjectScript_IsPreserved()
        {
            var target = Path.Combine(_root, "RuntimeProjectLifetimeScope.cs");
            var template = Path.Combine(_root, "RuntimeProjectLifetimeScope.cs.txt");
            File.WriteAllText(target, "project-owned");
            File.WriteAllText(template, "starter");

            var plan = ScaffoldPlanBuilder.Build(
                new[] { (target, template) },
                Array.Empty<string>());

            Assert.That(plan.Items.Single().Kind, Is.EqualTo(ScaffoldChangeKind.Preserve));
        }

        [Test]
        public void ExistingScaffoldAsset_IsReportedAsUpdate()
        {
            var asset = Path.Combine(_root, "GameplayScene.unity");
            File.WriteAllText(asset, "existing-scene");

            var plan = ScaffoldPlanBuilder.Build(
                Array.Empty<(string Target, string Template)>(),
                new[] { asset });

            Assert.That(plan.Items.Single().Kind, Is.EqualTo(ScaffoldChangeKind.Update));
        }
    }
}
