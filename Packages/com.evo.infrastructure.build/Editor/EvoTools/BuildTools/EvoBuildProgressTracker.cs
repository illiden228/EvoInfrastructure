using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    internal sealed class EvoBuildProgressTracker : IDisposable
    {
        private readonly Stopwatch totalStopwatch = Stopwatch.StartNew();
        private readonly List<Entry> entries = new();
        private readonly string title;
        private bool disposed;

        public EvoBuildProgressTracker(string valueTitle)
        {
            title = string.IsNullOrWhiteSpace(valueTitle) ? "Evo Build" : valueTitle;
        }

        public IDisposable Step(string label, float progress, EvoBuildApplyResult result = null)
        {
            label = string.IsNullOrWhiteSpace(label) ? "Build step" : label;
            progress = Mathf.Clamp01(progress);
            EditorUtility.DisplayProgressBar(title, label, progress);
            Debug.Log($"[Evo Build] Started: {label}");
            return new Scope(this, label, result);
        }

        public string CreateReport(
            PlatformBuildProfile profile,
            string outputPath,
            bool buildAndRun,
            EvoBuildApplyResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Evo Build Report");
            builder.AppendLine($"Created At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Total Time: {FormatElapsed(totalStopwatch.Elapsed)}");
            builder.AppendLine($"Profile: {profile?.ProfileId ?? "<none>"}");
            builder.AppendLine($"Display Name: {profile?.DisplayName ?? "<none>"}");
            builder.AppendLine($"Platform: {profile?.PlatformId ?? "<none>"}");
            builder.AppendLine($"Target: {profile?.BuildTargetGroup}/{profile?.BuildTarget}");
            builder.AppendLine($"Build And Run: {buildAndRun}");
            builder.AppendLine($"Output: {outputPath ?? string.Empty}");
            builder.AppendLine($"Bundle Version: {PlayerSettings.bundleVersion}");
            if (profile != null && profile.BuildTarget == BuildTarget.Android)
            {
                builder.AppendLine($"Android Version Code: {PlayerSettings.Android.bundleVersionCode}");
                builder.AppendLine($"Android Package: {(EditorUserBuildSettings.buildAppBundle ? "AAB" : "APK")}");
            }

            builder.AppendLine();
            builder.AppendLine("Timings:");
            for (var i = 0; i < entries.Count; i++)
            {
                builder.AppendLine($"- {entries[i].Label}: {FormatElapsed(entries[i].Elapsed)}");
            }

            builder.AppendLine();
            builder.AppendLine("Messages:");
            AppendList(builder, result?.Messages);

            builder.AppendLine();
            builder.AppendLine("Errors:");
            AppendList(builder, result?.Errors);

            return builder.ToString();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            EditorUtility.ClearProgressBar();
        }

        private void Complete(string label, TimeSpan elapsed, EvoBuildApplyResult result)
        {
            entries.Add(new Entry(label, elapsed));
            var message = $"{label} completed in {FormatElapsed(elapsed)}.";
            result?.AddMessage(message);
            Debug.Log($"[Evo Build] {message}");
        }

        private static void AppendList(StringBuilder builder, IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                builder.AppendLine("- None");
                return;
            }

            for (var i = 0; i < values.Count; i++)
            {
                builder.AppendLine($"- {values[i]}");
            }
        }

        private static string FormatElapsed(TimeSpan elapsed)
        {
            return elapsed.TotalHours >= 1d
                ? elapsed.ToString(@"hh\:mm\:ss\.fff")
                : elapsed.ToString(@"mm\:ss\.fff");
        }

        private readonly struct Entry
        {
            public Entry(string label, TimeSpan elapsed)
            {
                Label = label;
                Elapsed = elapsed;
            }

            public string Label { get; }
            public TimeSpan Elapsed { get; }
        }

        private sealed class Scope : IDisposable
        {
            private readonly EvoBuildProgressTracker owner;
            private readonly string label;
            private readonly EvoBuildApplyResult result;
            private readonly Stopwatch stopwatch;
            private bool disposed;

            public Scope(EvoBuildProgressTracker valueOwner, string valueLabel, EvoBuildApplyResult valueResult)
            {
                owner = valueOwner;
                label = valueLabel;
                result = valueResult;
                stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                stopwatch.Stop();
                owner.Complete(label, stopwatch.Elapsed, result);
            }
        }
    }
}
