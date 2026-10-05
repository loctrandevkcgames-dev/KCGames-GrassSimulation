using System;
using System.IO;
using System.Text.RegularExpressions;
using EncosyTower.Serialization.NewtonsoftJson;
using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GrassSimulation.Progression.Tests;

public sealed class FileProgressStoreTests
{
    private const string VALID_JSON = "{\"Version\":1,\"Coins\":40}";
    private const string OTHER_VALID_JSON = "{\"Version\":1,\"Coins\":7}";
    private const string FUTURE_JSON = "{\"Version\":99,\"Coins\":5}";
    private const string GARBAGE = "{ not json";

    private static readonly LevelId s_level = new("level-01");

    private string _directory;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GrassProgressTests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
        else if (File.Exists(_directory))
        {
            File.Delete(_directory);
        }
    }

    [Test]
    public void SaveThenLoad_RoundTrips()
    {
        var store = new FileProgressStore(_directory);
        var save = ProgressSave.CreateNew();

        save.Coins = 125;
        save.CompletedLevels.Add(s_level.Value);
        save.BestStars[s_level.Value] = 2;
        save.GrantedRewards.Add(new RewardId.FirstWin(s_level).ToKey());
        save.OwnedSkins.Add("skin-a");

        Assert.That(store.Save(save).IsSuccess, Is.True);
        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);

        Assert.That(loaded.Version, Is.EqualTo(ProgressSave.CURRENT_VERSION));
        Assert.That(loaded.Coins, Is.EqualTo(125));
        Assert.That(loaded.CompletedLevels, Is.EquivalentTo(new[] { s_level.Value }));
        Assert.That(loaded.BestStars[s_level.Value], Is.EqualTo(2));
        Assert.That(loaded.GrantedRewards, Is.EquivalentTo(new[] { "first-win:level-01" }));
        Assert.That(loaded.OwnedSkins, Is.EquivalentTo(new[] { "skin-a" }));
    }

    [Test]
    public void NothingOnDisk_LoadsNotFound()
    {
        var store = new FileProgressStore(_directory);

        Assert.That(LoadErrorOf(store), Is.EqualTo(LoadError.EnumCase.NotFound));
    }

    [Test]
    public void SaveWithAnExistingDirectoryMissing_CreatesIt()
    {
        var store = new FileProgressStore(Path.Combine(_directory, "nested"));

        Assert.That(store.Save(ProgressSave.CreateNew()).IsSuccess, Is.True);
        Assert.That(File.Exists(Path.Combine(_directory, "nested", "progress.json")), Is.True);
    }

    [Test]
    public void RestartOnTheSameResult_PaysNothingMore()
    {
        var first = CreateService();

        first.Settle(s_level, LevelResults.Win(stars: 3));

        var restarted = CreateService();

        Assert.That(restarted.Coins, Is.EqualTo(175));
        Assert.That(restarted.Settle(s_level, LevelResults.Win(stars: 3)).TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.CoinsGranted, Is.Zero);
        Assert.That(restarted.Coins, Is.EqualTo(175));
    }

    [Test]
    public void ReloadedLedger_DoesNotDoublePay()
    {
        var service = CreateService();

        service.Settle(s_level, LevelResults.Win(stars: 2));
        service.Settle(s_level, LevelResults.Win(stars: 3));
        service.Reload();

        Assert.That(service.Coins, Is.EqualTo(175));
        Assert.That(service.Settle(s_level, LevelResults.Win(stars: 3)).TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.CoinsGranted, Is.Zero);
    }

    [Test]
    public void OnlyTempFilePresent_Recovers()
    {
        Write("progress.json.tmp", VALID_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Coins, Is.EqualTo(40));
    }

    [Test]
    public void GarbageTempFileWithValidMain_LoadsMain()
    {
        Write("progress.json", VALID_JSON);
        Write("progress.json.tmp", GARBAGE);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Coins, Is.EqualTo(40));
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json")), Is.True);
    }

    [Test]
    public void CorruptMainWithValidBackup_LoadsBackupAndQuarantinesMain()
    {
        Write("progress.json", GARBAGE);
        Write("progress.json.bak", OTHER_VALID_JSON);
        ExpectJsonException();

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Coins, Is.EqualTo(7));
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json")), Is.False);

        var quarantined = Directory.GetFiles(_directory, "progress.json.corrupt-*");

        Assert.That(quarantined.Length, Is.EqualTo(1));
        Assert.That(File.ReadAllText(quarantined[0]), Is.EqualTo(GARBAGE));
    }

    [Test]
    public void AllFilesCorrupt_LoadsCorruptAndOverwritesNothing()
    {
        Write("progress.json", GARBAGE);
        Write("progress.json.bak", GARBAGE + "1");
        Write("progress.json.tmp", GARBAGE + "2");
        ExpectJsonException(count: 3);

        var store = new FileProgressStore(_directory);

        Assert.That(LoadErrorOf(store), Is.EqualTo(LoadError.EnumCase.Corrupt));
        Assert.That(File.ReadAllText(Path.Combine(_directory, "progress.json.bak")), Is.EqualTo(GARBAGE + "1"));
        Assert.That(File.ReadAllText(Path.Combine(_directory, "progress.json.tmp")), Is.EqualTo(GARBAGE + "2"));

        var quarantined = Directory.GetFiles(_directory, "progress.json.corrupt-*");

        Assert.That(quarantined.Length, Is.EqualTo(1));
        Assert.That(File.ReadAllText(quarantined[0]), Is.EqualTo(GARBAGE));
    }

    [Test]
    public void VersionAboveCurrent_IsQuarantinedNotDeleted()
    {
        Write("progress.json", FUTURE_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(LoadErrorOf(store), Is.EqualTo(LoadError.EnumCase.Corrupt));
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json")), Is.False);

        var quarantined = Directory.GetFiles(_directory, "progress.json.corrupt-*");

        Assert.That(quarantined.Length, Is.EqualTo(1));
        Assert.That(File.ReadAllText(quarantined[0]), Is.EqualTo(FUTURE_JSON));
    }

    [Test]
    public void SaveAfterAQuarantinedFutureVersion_KeepsTheQuarantinedFile()
    {
        Write("progress.json", FUTURE_JSON);

        var store = new FileProgressStore(_directory);

        store.Load();

        Assert.That(store.Save(ProgressSave.CreateNew()).IsSuccess, Is.True);
        Assert.That(store.Load().TryGetValue(out _), Is.True);

        var quarantined = Directory.GetFiles(_directory, "progress.json.corrupt-*");

        Assert.That(quarantined.Length, Is.EqualTo(1));
        Assert.That(File.ReadAllText(quarantined[0]), Is.EqualTo(FUTURE_JSON));
    }

    [Test]
    public void SaveOverAFutureVersionMain_QuarantinesItInsteadOfOverwriting()
    {
        Write("progress.json", FUTURE_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Save(ProgressSave.CreateNew()).IsSuccess, Is.True);

        var quarantined = Directory.GetFiles(_directory, "progress.json.corrupt-*");

        Assert.That(quarantined.Length, Is.EqualTo(1));
        Assert.That(File.ReadAllText(quarantined[0]), Is.EqualTo(FUTURE_JSON));
    }

    [Test]
    public void NegativeCoins_AreRejected()
    {
        Write("progress.json", "{\"Version\":1,\"Coins\":-5}");

        var store = new FileProgressStore(_directory);

        Assert.That(LoadErrorOf(store), Is.EqualTo(LoadError.EnumCase.Corrupt));
    }

    [Test]
    public void SaveOverACorruptMain_KeepsTheGoodBackup()
    {
        Write("progress.json", GARBAGE);
        Write("progress.json.bak", OTHER_VALID_JSON);
        ExpectJsonException();

        var store = new FileProgressStore(_directory);
        var save = ProgressSave.CreateNew();

        save.Coins = 125;

        Assert.That(store.Save(save).IsSuccess, Is.True);

        Assert.That(ReadSave("progress.json").Coins, Is.EqualTo(125));
        Assert.That(ReadSave("progress.json.bak").Coins, Is.EqualTo(7));

        var quarantined = Directory.GetFiles(_directory, "progress.json.corrupt-*");

        Assert.That(quarantined.Length, Is.EqualTo(1));
        Assert.That(File.ReadAllText(quarantined[0]), Is.EqualTo(GARBAGE));
    }

    [Test]
    public void RepeatedSaves_KeepTheLatestInMainAndThePreviousInBackup()
    {
        var store = new FileProgressStore(_directory);

        for (var coins = 10; coins <= 40; coins += 10)
        {
            var save = ProgressSave.CreateNew();

            save.Coins = coins;

            Assert.That(store.Save(save).IsSuccess, Is.True);
        }

        Assert.That(ReadSave("progress.json").Coins, Is.EqualTo(40));
        Assert.That(ReadSave("progress.json.bak").Coins, Is.EqualTo(30));
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.tmp")), Is.False);
        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Coins, Is.EqualTo(40));
    }

    [Test]
    public void MainMissing_LoadsTheNewestRevisionOfBackupAndTemp()
    {
        Write("progress.json.bak", "{\"Version\":1,\"Revision\":3,\"Coins\":7}");
        Write("progress.json.tmp", "{\"Version\":1,\"Revision\":4,\"Coins\":40}");

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Coins, Is.EqualTo(40));
    }

    [Test]
    public void MainMissing_TempWithAnOlderRevisionLosesToTheBackup()
    {
        Write("progress.json.bak", "{\"Version\":1,\"Revision\":5,\"Coins\":7}");
        Write("progress.json.tmp", "{\"Version\":1,\"Revision\":4,\"Coins\":40}");

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Coins, Is.EqualTo(7));
    }

    [Test]
    public void MainMissing_EqualRevisionsPreferTheTemp()
    {
        Write("progress.json.bak", "{\"Version\":1,\"Revision\":2,\"Coins\":7}");
        Write("progress.json.tmp", "{\"Version\":1,\"Revision\":2,\"Coins\":40}");

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Coins, Is.EqualTo(40));
    }

    [Test]
    public void FilesWithoutARevision_ReadAsRevisionZero()
    {
        Write("progress.json", VALID_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Revision, Is.Zero);
    }

    [Test]
    public void SaveWithALockedMain_FailsAndLeavesNoTempFile()
    {
        Write("progress.json", VALID_JSON);

        var store = new FileProgressStore(_directory);

        using (LockMain())
        {
            var outcome = store.Save(ProgressSave.CreateNew());

            Assert.That(outcome.TryGetFailure(out var failure), Is.True);
            Assert.That(failure.GetEnumCase(), Is.EqualTo(SaveError.EnumCase.WriteFailed));
        }

        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.tmp")), Is.False);
        Assert.That(File.ReadAllText(Path.Combine(_directory, "progress.json")), Is.EqualTo(VALID_JSON));
    }

    [Test]
    public void SaveAfterAnAllCorruptLoad_LeavesTheQuarantinedFileInPlace()
    {
        Write("progress.json", GARBAGE);
        Write("progress.json.bak", GARBAGE + "1");
        ExpectJsonException(count: 2);

        var store = new FileProgressStore(_directory);

        Assert.That(LoadErrorOf(store), Is.EqualTo(LoadError.EnumCase.Corrupt));
        Assert.That(store.Save(ProgressSave.CreateNew()).IsSuccess, Is.True);

        var quarantined = Directory.GetFiles(_directory, "progress.json.corrupt-*");

        Assert.That(quarantined.Length, Is.EqualTo(1));
        Assert.That(File.ReadAllText(quarantined[0]), Is.EqualTo(GARBAGE));
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.tmp")), Is.False);
        Assert.That(store.Load().TryGetValue(out _), Is.True);
    }

    [Test]
    public void UnreadableMain_LoadsUnreadableAndIsNeitherQuarantinedNorFallenBack()
    {
        Write("progress.json", VALID_JSON);
        Write("progress.json.bak", OTHER_VALID_JSON);

        var store = new FileProgressStore(_directory);

        using (LockMain())
        {
            Assert.That(LoadErrorOf(store), Is.EqualTo(LoadError.EnumCase.Unreadable));
        }

        Assert.That(File.ReadAllText(Path.Combine(_directory, "progress.json")), Is.EqualTo(VALID_JSON));
        Assert.That(Directory.GetFiles(_directory, "progress.json.corrupt-*"), Is.Empty);
    }

    [Test]
    public void UnreadableMain_MakesTheServiceReadOnlyAndSettleLeavesTheFilesAlone()
    {
        Write("progress.json", VALID_JSON);

        var service = new ProgressionService(new FileProgressStore(_directory));

        using (LockMain())
        {
            service.Initialize();

            LogAssert.Expect(LogType.Warning, new Regex("read-only"));

            var outcome = service.Settle(s_level, LevelResults.Win(stars: 3));

            Assert.That(service.IsReadOnly, Is.True);
            Assert.That(outcome.TryGetError(out var error), Is.True);
            Assert.That(error.GetEnumCase(), Is.EqualTo(SettleError.EnumCase.StoreUnavailable));
        }

        Assert.That(File.ReadAllText(Path.Combine(_directory, "progress.json")), Is.EqualTo(VALID_JSON));
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.bak")), Is.False);
        Assert.That(Directory.GetFiles(_directory, "progress.json.corrupt-*"), Is.Empty);
    }

    [Test]
    public void WipeThenCorruptMain_DoesNotResurrectThePreWipeProgress()
    {
        var service = CreateService();

        service.Settle(s_level, LevelResults.Win(stars: 1));
        service.Settle(s_level, LevelResults.Win(stars: 3));

        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.bak")), Is.True);
        Assert.That(service.Wipe().IsSuccess, Is.True);

        Write("progress.json", GARBAGE);
        ExpectJsonException();

        var store = new FileProgressStore(_directory);
        var loaded = store.Load();

        Assert.That(loaded.IsSuccess, Is.False);
        Assert.That(LoadErrorOf(loaded), Is.EqualTo(LoadError.EnumCase.Corrupt));
    }

    [Test]
    public void Delete_RemovesTheSaveFilesAndKeepsTheQuarantinedOnes()
    {
        Write("progress.json", VALID_JSON);
        Write("progress.json.bak", OTHER_VALID_JSON);
        Write("progress.json.tmp", VALID_JSON);
        Write("progress.json.corrupt-1", GARBAGE);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Delete().IsSuccess, Is.True);

        Assert.That(File.Exists(Path.Combine(_directory, "progress.json")), Is.False);
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.bak")), Is.False);
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.tmp")), Is.False);
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.corrupt-1")), Is.True);
        Assert.That(LoadErrorOf(store), Is.EqualTo(LoadError.EnumCase.NotFound));
    }

    [Test]
    public void DeleteWithNothingOnDisk_Succeeds()
    {
        var store = new FileProgressStore(_directory);

        Assert.That(store.Delete().IsSuccess, Is.True);
    }

    [Test]
    public void DeleteOfALockedMain_FailsWithoutThrowing()
    {
        Write("progress.json", VALID_JSON);

        var store = new FileProgressStore(_directory);

        using (LockMain())
        {
            LogAssert.Expect(LogType.Error, new Regex("Cannot delete the progress save"));

            var outcome = store.Delete();

            Assert.That(outcome.TryGetFailure(out var failure), Is.True);
            Assert.That(failure.GetEnumCase(), Is.EqualTo(SaveError.EnumCase.WriteFailed));
        }
    }

    [Test]
    public void SaveWhereTheDirectoryIsAFile_FailsWithoutThrowing()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_directory));
        File.WriteAllText(_directory, "not a directory");

        var store = new FileProgressStore(_directory);

        LogAssert.Expect(LogType.Error, new Regex("Cannot write the progress save"));

        var outcome = store.Save(ProgressSave.CreateNew());

        Assert.That(outcome.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.GetEnumCase(), Is.EqualTo(SaveError.EnumCase.WriteFailed));
    }

    private static LoadError.EnumCase LoadErrorOf(FileProgressStore store)
        => LoadErrorOf(store.Load());

    private static LoadError.EnumCase LoadErrorOf(EncosyTower.Common.Result<ProgressSave, LoadError> result)
        => result.TryGetError(out var error) ? error.GetEnumCase() : LoadError.EnumCase.Undefined;

    private static void ExpectJsonException(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            LogAssert.Expect(LogType.Exception, new Regex("JsonReaderException"));
        }
    }

    private FileStream LockMain()
        => new(Path.Combine(_directory, "progress.json"), FileMode.Open, FileAccess.Read, FileShare.None);

    private ProgressionService CreateService()
    {
        var service = new ProgressionService(new FileProgressStore(_directory));

        service.Initialize();
        return service;
    }

    private ProgressSave ReadSave(string fileName)
    {
        var json = File.ReadAllText(Path.Combine(_directory, fileName));

        Assert.That(JsonHelper.TryDeserialize<ProgressSave>(json, out var save), Is.True);
        return save;
    }

    private void Write(string fileName, string content)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, fileName), content);
    }
}
