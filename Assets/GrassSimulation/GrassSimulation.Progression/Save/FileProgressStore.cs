using System;
using System.IO;
using System.Text;
using EncosyTower.Common;
using EncosyTower.Serialization.NewtonsoftJson;

namespace GrassSimulation.Progression
{
    public sealed class FileProgressStore : IProgressStore
    {
        private const string MAIN_FILE = "progress.json";
        private const string TEMP_FILE = "progress.json.tmp";
        private const string BACKUP_FILE = "progress.json.bak";
        private const string CORRUPT_PREFIX = "progress.json.corrupt-";

        private readonly string _directory;
        private readonly string _mainPath;
        private readonly string _tempPath;
        private readonly string _backupPath;

        public FileProgressStore(string directory)
        {
            _directory = directory;
            _mainPath = Path.Combine(directory, MAIN_FILE);
            _tempPath = Path.Combine(directory, TEMP_FILE);
            _backupPath = Path.Combine(directory, BACKUP_FILE);
        }

        private static ReadStatus Read(string path, out ProgressSave save, out string reason)
        {
            save = null;
            reason = string.Empty;

            string json;

            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (FileNotFoundException)
            {
                return ReadStatus.Missing;
            }
            catch (DirectoryNotFoundException)
            {
                return ReadStatus.Missing;
            }
            catch (Exception exception)
            {
                ThrowHelper.LogWarning_ReadFailed(path, exception);
                reason = exception.Message;
                return ReadStatus.Unreadable;
            }

            if (JsonHelper.TryDeserialize<ProgressSave>(json, out var loaded) == false
                || loaded == null
                || IsValid(loaded) == false
                || ProgressMigration.TryUpgrade(loaded) == false
            )
            {
                ThrowHelper.LogWarning_InvalidSave(path);
                return ReadStatus.Corrupt;
            }

            save = loaded;
            return ReadStatus.Valid;
        }

        private static bool IsValid(ProgressSave save)
        {
            return save.Version >= 1
                && save.Version <= ProgressSave.CURRENT_VERSION
                && save.Revision >= 0
                && save.Coins >= 0
                && save.CompletedLevels != null
                && save.BestStars != null
                && save.GrantedRewards != null
                && save.OwnedSkins != null;
        }

        private static bool TryQuarantine(string path)
        {
            var quarantinePath = Path.Combine(Path.GetDirectoryName(path), CORRUPT_PREFIX + DateTime.UtcNow.Ticks);

            try
            {
                File.Move(path, quarantinePath);
                ThrowHelper.LogWarning_Quarantined(path, quarantinePath);
                return true;
            }
            catch (Exception exception)
            {
                ThrowHelper.LogWarning_QuarantineFailed(path, exception);
                return false;
            }
        }

        private static void TryDelete(string path, ref Exception failure, ref string failedPath)
        {
            try
            {
                File.Delete(path);
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (Exception exception)
            {
                ThrowHelper.LogError_DeleteFailed(path, exception);
                failure ??= exception;
                failedPath ??= path;
            }
        }

        public Result<ProgressSave, LoadError> Load()
        {
            try
            {
                return LoadCore();
            }
            catch (Exception exception)
            {
                ThrowHelper.LogWarning_ReadFailed(_mainPath, exception);
                return Result<ProgressSave, LoadError>.Err(new LoadError.Unreadable(_mainPath, exception.Message));
            }
        }

        public Success<SaveError> Save(ProgressSave save)
        {
            try
            {
                return SaveCore(save);
            }
            catch (Exception exception)
            {
                ThrowHelper.LogError_SaveFailed(_mainPath, exception);
                return Success.No<SaveError>(new SaveError.WriteFailed(_mainPath, exception.Message));
            }
        }

        public Success<SaveError> Delete()
        {
            Exception failure = null;
            string failedPath = null;

            TryDelete(_mainPath, ref failure, ref failedPath);
            TryDelete(_backupPath, ref failure, ref failedPath);
            TryDelete(_tempPath, ref failure, ref failedPath);

            if (failure != null)
            {
                return Success.No<SaveError>(new SaveError.WriteFailed(failedPath, failure.Message));
            }

            return Success.Yes;
        }

        private Result<ProgressSave, LoadError> LoadCore()
        {
            string corruptPath = null;

            var mainStatus = Read(_mainPath, out var mainSave, out var mainReason);

            if (mainStatus == ReadStatus.Valid)
            {
                return Result<ProgressSave, LoadError>.Succeed(mainSave);
            }

            if (mainStatus == ReadStatus.Unreadable)
            {
                return Result<ProgressSave, LoadError>.Err(new LoadError.Unreadable(_mainPath, mainReason));
            }

            if (mainStatus == ReadStatus.Corrupt)
            {
                corruptPath = _mainPath;
                TryQuarantine(_mainPath);
            }

            var backupStatus = Read(_backupPath, out var backupSave, out var backupReason);

            if (backupStatus == ReadStatus.Unreadable)
            {
                return Result<ProgressSave, LoadError>.Err(new LoadError.Unreadable(_backupPath, backupReason));
            }

            var tempStatus = Read(_tempPath, out var tempSave, out var tempReason);

            if (tempStatus == ReadStatus.Unreadable)
            {
                return Result<ProgressSave, LoadError>.Err(new LoadError.Unreadable(_tempPath, tempReason));
            }

            var hasBackup = backupStatus == ReadStatus.Valid;
            var hasTemp = tempStatus == ReadStatus.Valid;

            if (hasBackup && hasTemp)
            {
                var newest = tempSave.Revision >= backupSave.Revision ? tempSave : backupSave;

                return Result<ProgressSave, LoadError>.Succeed(newest);
            }

            if (hasTemp)
            {
                return Result<ProgressSave, LoadError>.Succeed(tempSave);
            }

            if (hasBackup)
            {
                return Result<ProgressSave, LoadError>.Succeed(backupSave);
            }

            if (corruptPath == null && backupStatus == ReadStatus.Corrupt)
            {
                corruptPath = _backupPath;
            }

            if (corruptPath == null && tempStatus == ReadStatus.Corrupt)
            {
                corruptPath = _tempPath;
            }

            if (corruptPath != null)
            {
                return Result<ProgressSave, LoadError>.Err(new LoadError.Corrupt(corruptPath));
            }

            return Result<ProgressSave, LoadError>.Err(new LoadError.NotFound());
        }

        private Success<SaveError> SaveCore(ProgressSave save)
        {
            if (JsonHelper.TrySerialize(save, out var json) == false)
            {
                ThrowHelper.LogError_SerializeFailed(_mainPath);
                return Success.No<SaveError>(new SaveError.SerializeFailed());
            }

            var mainStatus = Read(_mainPath, out _, out var reason);

            if (mainStatus == ReadStatus.Unreadable)
            {
                return Success.No<SaveError>(new SaveError.WriteFailed(_mainPath, reason));
            }

            if (mainStatus == ReadStatus.Corrupt && TryQuarantine(_mainPath) == false)
            {
                return Success.No<SaveError>(
                    new SaveError.WriteFailed(_mainPath, "The corrupt save cannot be quarantined")
                );
            }

            var swapped = false;

            try
            {
                Directory.CreateDirectory(_directory);

                using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var bytes = Encoding.UTF8.GetBytes(json);

                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(flushToDisk: true);
                }

                if (mainStatus == ReadStatus.Valid)
                {
                    File.Replace(_tempPath, _mainPath, _backupPath);
                }
                else
                {
                    File.Move(_tempPath, _mainPath);
                }

                swapped = true;
                return Success.Yes;
            }
            finally
            {
                if (swapped == false)
                {
                    Exception ignoredFailure = null;
                    string ignoredPath = null;

                    TryDelete(_tempPath, ref ignoredFailure, ref ignoredPath);
                }
            }
        }

        private enum ReadStatus
        {
            Missing,
            Valid,
            Unreadable,
            Corrupt,
        }
    }
}
