using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Debug;
using UnityEngine;

namespace Evo.Infrastructure.Services.Save
{
    public sealed class PrefsSaveBackend : ISaveBackend
    {
        private readonly string _saveKey;

        public PrefsSaveBackend(SaveStorageOptions options = null)
        {
            _saveKey = !string.IsNullOrWhiteSpace(options?.playerPrefsKey)
                ? options.playerPrefsKey
                : SaveStorageDefaults.PlayerPrefsKey;
        }

        public string BackendId => "prefs";
        public int Priority => 10;
        public bool IsAvailable => true;

        public async UniTask<SaveEnvelope> LoadAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            if (!PlayerPrefs.HasKey(_saveKey))
            {
                return null;
            }

            var json = PlayerPrefs.GetString(_saveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveEnvelope>(json);
        }

        public async UniTask<bool> SaveAsync(SaveEnvelope envelope, System.Threading.CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            var json = JsonUtility.ToJson(envelope);
            PlayerPrefs.SetString(_saveKey, json);
            PlayerPrefs.Save();
            return true;
        }
    }

    public sealed class FileSaveBackend : ISaveBackend
    {
        private const int MAX_IO_ATTEMPTS = 3;
        private const int IO_RETRY_DELAY_MS = 50;
        private static readonly SemaphoreSlim FileIoLock = new(1, 1);
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
            return Path.Combine(UnityEngine.Application.persistentDataPath, _fileName);
        }

        public async UniTask<SaveEnvelope> LoadAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            var filePath = BuildFilePath();

            string json;
            try
            {
                await FileIoLock.WaitAsync(cancellationToken);
                try
                {
                    await UniTask.SwitchToThreadPool();
                    if (!File.Exists(filePath))
                    {
                        await UniTask.SwitchToMainThread(cancellationToken);
                        return null;
                    }

                    json = ReadAllTextWithRetries(filePath);
                    await UniTask.SwitchToMainThread();
                }
                finally
                {
                    FileIoLock.Release();
                }
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
                await FileIoLock.WaitAsync(cancellationToken);
                try
                {
                    await UniTask.SwitchToThreadPool();
                    WriteAllTextAtomicWithRetries(filePath, json);
                    await UniTask.SwitchToMainThread(cancellationToken);
                }
                finally
                {
                    FileIoLock.Release();
                }
            }
            catch (System.Exception ex)
            {
                await UniTask.SwitchToMainThread(cancellationToken);
                EvoDebug.LogWarning($"File save failed at '{filePath}': {ex.Message}", nameof(FileSaveBackend));
                return false;
            }
            return true;
        }

        private static string ReadAllTextWithRetries(string filePath)
        {
            for (var attempt = 1; attempt <= MAX_IO_ATTEMPTS; attempt++)
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                catch (IOException) when (attempt < MAX_IO_ATTEMPTS)
                {
                    Thread.Sleep(IO_RETRY_DELAY_MS);
                }
            }

            using var finalStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var finalReader = new StreamReader(finalStream);
            return finalReader.ReadToEnd();
        }

        private static void WriteAllTextAtomicWithRetries(string filePath, string json)
        {
            var tempPath = filePath + ".tmp";
            for (var attempt = 1; attempt <= MAX_IO_ATTEMPTS; attempt++)
            {
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
                    Thread.Sleep(IO_RETRY_DELAY_MS);
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
