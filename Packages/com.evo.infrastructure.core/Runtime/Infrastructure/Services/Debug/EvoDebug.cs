using System;
using System.Diagnostics;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Debug
{
    public interface IEvoDebugSink
    {
        void OnLog(LogType type, string message, UnityEngine.Object context, string stackTrace);
    }

    public enum EvoStackTrace
    {
        Full = 0,
        ScriptOnly = 1,
        None = 2
    }

    public static class EvoDebug
    {
#if FULL_LOG
        private static IEvoDebugSink _sink;
        private static bool _includeStackTraceInSink = true;
        private static bool _useColors = true;
        private static string _infoColor = "#FFFFFF";
        private static string _warningColor = "#F5D76E";
        private static string _errorColor = "#FF8A80";
        private static string _sourceColor = "#80DEEA";

        public static void SetSink(IEvoDebugSink sink)
        {
            _sink = sink;
        }

        public static void SetStackTrace(EvoStackTrace mode)
        {
            var unityMode = mode switch
            {
                EvoStackTrace.Full => StackTraceLogType.Full,
                EvoStackTrace.ScriptOnly => StackTraceLogType.ScriptOnly,
                EvoStackTrace.None => StackTraceLogType.None,
                _ => StackTraceLogType.Full
            };

            UnityEngine.Application.SetStackTraceLogType(LogType.Log, unityMode);
            UnityEngine.Application.SetStackTraceLogType(LogType.Warning, unityMode);
            UnityEngine.Application.SetStackTraceLogType(LogType.Error, unityMode);
            UnityEngine.Application.SetStackTraceLogType(LogType.Assert, unityMode);
            UnityEngine.Application.SetStackTraceLogType(LogType.Exception, unityMode);
        }

        public static void SetColors(
            bool enabled,
            string infoColor = null,
            string warningColor = null,
            string errorColor = null,
            string sourceColor = null)
        {
            _useColors = enabled;
            if (!string.IsNullOrEmpty(infoColor))
            {
                _infoColor = infoColor;
            }

            if (!string.IsNullOrEmpty(warningColor))
            {
                _warningColor = warningColor;
            }

            if (!string.IsNullOrEmpty(errorColor))
            {
                _errorColor = errorColor;
            }

            if (!string.IsNullOrEmpty(sourceColor))
            {
                _sourceColor = sourceColor;
            }
        }

        public static void SetIncludeStackTraceInSink(bool include)
        {
            _includeStackTraceInSink = include;
        }

        [Conditional("FULL_LOG")]
        public static void Log(string message, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, null, LogType.Log, null, false);
            UnityEngine.Debug.Log(finalMessage, context);
            Forward(LogType.Log, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogWarning(string message, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, null, LogType.Warning, null, false);
            UnityEngine.Debug.LogWarning(finalMessage, context);
            Forward(LogType.Warning, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogError(string message, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, null, LogType.Error, null, false);
            UnityEngine.Debug.LogError(finalMessage, context);
            Forward(LogType.Error, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogException(Exception exception, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogException(exception, context);
            var finalMessage = FormatMessage(
                exception != null ? exception.ToString() : "Exception is null",
                null,
                LogType.Exception,
                null,
                false);
            Forward(LogType.Exception, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void Assert(bool condition, string message, UnityEngine.Object context = null)
        {
            if (condition)
            {
                return;
            }

            var finalMessage = FormatMessage(message, null, LogType.Assert, null, false);
            UnityEngine.Debug.LogError(finalMessage, context);
            Forward(LogType.Assert, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void Log(string message, string source, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, source, LogType.Log, null, false);
            UnityEngine.Debug.Log(finalMessage, context);
            Forward(LogType.Log, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogWarning(string message, string source, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, source, LogType.Warning, null, false);
            UnityEngine.Debug.LogWarning(finalMessage, context);
            Forward(LogType.Warning, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogError(string message, string source, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, source, LogType.Error, null, false);
            UnityEngine.Debug.LogError(finalMessage, context);
            Forward(LogType.Error, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void Assert(bool condition, string message, string source, UnityEngine.Object context = null)
        {
            if (condition)
            {
                return;
            }

            var finalMessage = FormatMessage(message, source, LogType.Assert, null, false);
            UnityEngine.Debug.LogError(finalMessage, context);
            Forward(LogType.Assert, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogColored(string message, string colorHex, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, null, LogType.Log, colorHex, true);
            UnityEngine.Debug.Log(finalMessage, context);
            Forward(LogType.Log, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogWarningColored(string message, string colorHex, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, null, LogType.Warning, colorHex, true);
            UnityEngine.Debug.LogWarning(finalMessage, context);
            Forward(LogType.Warning, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogErrorColored(string message, string colorHex, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, null, LogType.Error, colorHex, true);
            UnityEngine.Debug.LogError(finalMessage, context);
            Forward(LogType.Error, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogColored(string message, string source, string colorHex, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, source, LogType.Log, colorHex, true);
            UnityEngine.Debug.Log(finalMessage, context);
            Forward(LogType.Log, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogWarningColored(string message, string source, string colorHex, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, source, LogType.Warning, colorHex, true);
            UnityEngine.Debug.LogWarning(finalMessage, context);
            Forward(LogType.Warning, finalMessage, context);
        }

        [Conditional("FULL_LOG")]
        public static void LogErrorColored(string message, string source, string colorHex, UnityEngine.Object context = null)
        {
            var finalMessage = FormatMessage(message, source, LogType.Error, colorHex, true);
            UnityEngine.Debug.LogError(finalMessage, context);
            Forward(LogType.Error, finalMessage, context);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeDefaults()
        {
            SetStackTrace(EvoStackTrace.Full);
        }

        private static void Forward(LogType type, string message, UnityEngine.Object context)
        {
            if (_sink == null)
            {
                return;
            }

            string stackTrace = null;
            if (_includeStackTraceInSink)
            {
                stackTrace = StackTraceUtility.ExtractStackTrace();
            }

            _sink.OnLog(type, message, context, stackTrace);
        }

        private static string FormatMessage(
            string message,
            string source,
            LogType type,
            string colorOverride,
            bool forceColor)
        {
            var finalMessage = string.IsNullOrEmpty(source)
                ? message
                : string.Concat("[", source, "] ", message);
            if (!_useColors && !forceColor)
            {
                return finalMessage;
            }

            var finalSource = source;
            if (!string.IsNullOrEmpty(source))
            {
                finalSource = string.Concat("<color=", _sourceColor, ">[", source, "]</color>");
            }

            var color = !string.IsNullOrEmpty(colorOverride)
                ? colorOverride
                : type switch
            {
                LogType.Warning => _warningColor,
                LogType.Error => _errorColor,
                LogType.Exception => _errorColor,
                LogType.Assert => _errorColor,
                _ => _infoColor
            };

            if (!string.IsNullOrEmpty(finalSource))
            {
                finalMessage = string.Concat(finalSource, " ", message);
            }

            return string.Concat("<color=", color, ">", finalMessage, "</color>");
        }
#else
        public static void SetSink(IEvoDebugSink sink) { }
        public static void SetStackTrace(EvoStackTrace mode) { }
        public static void SetColors(
            bool enabled,
            string infoColor = null,
            string warningColor = null,
            string errorColor = null,
            string sourceColor = null) { }
        public static void SetIncludeStackTraceInSink(bool include) { }
        public static void Log(string message, UnityEngine.Object context = null) { }
        public static void LogWarning(string message, UnityEngine.Object context = null) { }
        public static void LogError(string message, UnityEngine.Object context = null) { }
        public static void LogException(Exception exception, UnityEngine.Object context = null) { }
        public static void Assert(bool condition, string message, UnityEngine.Object context = null) { }
        public static void Log(string message, string source, UnityEngine.Object context = null) { }
        public static void LogWarning(string message, string source, UnityEngine.Object context = null) { }
        public static void LogError(string message, string source, UnityEngine.Object context = null) { }
        public static void Assert(bool condition, string message, string source, UnityEngine.Object context = null) { }
        public static void LogColored(string message, string colorHex, UnityEngine.Object context = null) { }
        public static void LogWarningColored(string message, string colorHex, UnityEngine.Object context = null) { }
        public static void LogErrorColored(string message, string colorHex, UnityEngine.Object context = null) { }
        public static void LogColored(string message, string source, string colorHex, UnityEngine.Object context = null) { }
        public static void LogWarningColored(string message, string source, string colorHex, UnityEngine.Object context = null) { }
        public static void LogErrorColored(string message, string source, string colorHex, UnityEngine.Object context = null) { }
#endif
    }
}
