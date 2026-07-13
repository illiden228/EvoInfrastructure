using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Core.Async;
using Evo.Infrastructure.Services.Debug;
using UnityEngine;

namespace Evo.Infrastructure.Services.Save
{
    public sealed class FileSaveBackend : ISaveBackend
    {
        private const int MAX_IO_ATTEMPTS = 3;
        private const int IO_RETRY_DELAY_MS = 50;
        private static readonly AsyncGate FileIoGate = new();
        private readonly string _fileName;

        public FileSaveBackend(SaveStorageOptions options = null)
        {
            _fileName = !string.IsNullOrWhiteSpace(options?.fileName)
                ? options.fileName
                : SaveStorageDefaults.FileName;
        }

        public string BackendId => "file";
        public int Priority => 20;
        public bool IsAvailable =>
#if UNITY_WEBGL && !UNITY_EDITOR
            false;
#else
            true;
#endif

        private string BuildFilePath()
        {
            return Path.Combine(Application.persistentDataPath, _fileName);
        }

        public async UniTask<SaveEnvelope> LoadAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            var filePath = BuildFilePath();

            string json;
            try
            {
                using var fileLease = await FileIoGate.EnterAsync(cancellationToken);
                try
                {
                    json = await ReadAllTextWithRetriesAsync(filePath, cancellationToken);
                }
                finally
                {
                    await UniTask.SwitchToMainThread(CancellationToken.None);
                }
            }
            catch (System.OperationCanceledException)
            {
                await UniTask.SwitchToMainThread(CancellationToken.None);
                throw;
            }
            catch (System.Exception ex)
            {
                await UniTask.SwitchToMainThread(cancellationToken);
                EvoDebug.LogWarning($"File load failed at '{filePath}': {ex.Message}", nameof(FileSaveBackend));
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveEnvelope>(json);
        }

        public async UniTask<bool> SaveAsync(SaveEnvelope envelope, System.Threading.CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            var filePath = BuildFilePath();
            var json = JsonUtility.ToJson(envelope);
            try
            {
                using var fileLease = await FileIoGate.EnterAsync(cancellationToken);
                try
                {
                    await WriteAllTextAtomicWithRetriesAsync(filePath, json, cancellationToken);
                }
                finally
                {
                    await UniTask.SwitchToMainThread(CancellationToken.None);
                }
            }
            catch (System.OperationCanceledException)
            {
                await UniTask.SwitchToMainThread(CancellationToken.None);
                throw;
            }
            catch (System.Exception ex)
            {
                await UniTask.SwitchToMainThread(cancellationToken);
                EvoDebug.LogWarning($"File save failed at '{filePath}': {ex.Message}", nameof(FileSaveBackend));
                return false;
            }

            return true;
        }

        private static async UniTask<string> ReadAllTextWithRetriesAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MAX_IO_ATTEMPTS; attempt++)
            {
                await UniTask.SwitchToThreadPool();
                try
                {
                    if (!File.Exists(filePath)) return null;
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                catch (IOException) when (attempt < MAX_IO_ATTEMPTS)
                {
                    await UniTask.SwitchToMainThread(cancellationToken);
                    await UniTask.Delay(IO_RETRY_DELAY_MS, cancellationToken: cancellationToken);
                }
            }

            using var finalStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var finalReader = new StreamReader(finalStream);
            return finalReader.ReadToEnd();
        }

        private static async UniTask WriteAllTextAtomicWithRetriesAsync(
            string filePath,
            string json,
            CancellationToken cancellationToken)
        {
            var tempPath = filePath + ".tmp";
            for (var attempt = 1; attempt <= MAX_IO_ATTEMPTS; attempt++)
            {
                await UniTask.SwitchToThreadPool();
                try
                {
                    using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(json);
                    }

                    if (File.Exists(filePath))
                    {
                        File.Replace(tempPath, filePath, null);
                    }
                    else
                    {
                        File.Move(tempPath, filePath);
                    }

                    return;
                }
                catch (IOException) when (attempt < MAX_IO_ATTEMPTS)
                {
                    await UniTask.SwitchToMainThread(cancellationToken);
                    await UniTask.Delay(IO_RETRY_DELAY_MS, cancellationToken: cancellationToken);
                }
            }

            using (var finalStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var finalWriter = new StreamWriter(finalStream))
            {
                finalWriter.Write(json);
            }

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
    }
}
