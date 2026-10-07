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
    private const string VALID_JSON = "{\"Version\":2,\"Levels\":{\"level-01\":{\"Attempts\":40}}}";
    private const string OTHER_VALID_JSON = "{\"Version\":2,\"Levels\":{\"level-01\":{\"Attempts\":7}}}";
    private const string FUTURE_JSON = "{\"Version\":99,\"Levels\":{}}";
    private const string GARBAGE = "{ not json";

    private const string V1_JSON = "{\"Version\":1,\"Revision\":2,\"Coins\":40,"
        + "\"CompletedLevels\":[\"level-01\",\"level-02\",\"level-03\"],"
        + "\"BestStars\":{\"level-01\":3,\"level-02\":2,\"level-04\":0},"
        + "\"GrantedRewards\":[\"first-win:level-01\"],\"OwnedSkins\":[\"skin-a\"]}";

    private const string V3_BOOSTER_JSON = "{\"Version\":3,\"Revision\":5,"
        + "\"Levels\":{\"level-04\":{\"Attempts\":1,\"Completed\":true,\"Stars\":1}},"
        + "\"OwnedMachines\":[\"standard\"],\"SelectedMachine\":\"standard\","
        + "\"GrantedUnlocks\":[\"level-04:ExtraTimeBooster\",\"level-06:TurboBooster\"]}";

    private const string V3_NO_BOOSTER_JSON = "{\"Version\":3,\"Revision\":1,"
        + "\"Levels\":{\"level-01\":{\"Attempts\":1}},"
        + "\"OwnedMachines\":[\"standard\"],\"SelectedMachine\":\"standard\",\"GrantedUnlocks\":[]}";

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

        save.Revision = 3;
        save.Levels[s_level.Value] = new LevelRecord {
            Stars = StarFlags.Goal | StarFlags.Side,
            Attempts = 4,
            Completed = true,
        };

        Assert.That(store.Save(save).IsSuccess, Is.True);
        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);

        Assert.That(loaded.Version, Is.EqualTo(ProgressSave.CURRENT_VERSION));
        Assert.That(loaded.Revision, Is.EqualTo(3));
        Assert.That(loaded.Levels[s_level.Value].Stars, Is.EqualTo(StarFlags.Goal | StarFlags.Side));
        Assert.That(loaded.Levels[s_level.Value].Attempts, Is.EqualTo(4));
        Assert.That(loaded.Levels[s_level.Value].Completed, Is.True);
        Assert.That(loaded.CompletedLevels, Is.Null);
        Assert.That(loaded.BestStars, Is.Null);
    }

    [Test]
    public void VersionOneSave_IsMigratedToStarFlags()
    {
        Write("progress.json", V1_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);

        Assert.That(loaded.Version, Is.EqualTo(ProgressSave.CURRENT_VERSION));
        Assert.That(loaded.Revision, Is.EqualTo(2));
        Assert.That(loaded.Levels["level-01"].Stars, Is.EqualTo(StarFlags.Goal | StarFlags.Clean | StarFlags.Side));
        Assert.That(loaded.Levels["level-01"].Completed, Is.True);
        Assert.That(loaded.Levels["level-02"].Stars, Is.EqualTo(StarFlags.Goal | StarFlags.Clean));
        Assert.That(loaded.Levels["level-03"].Stars, Is.EqualTo(StarFlags.Goal));
        Assert.That(loaded.Levels["level-03"].Completed, Is.True);
        Assert.That(loaded.Levels["level-04"].Stars, Is.EqualTo(StarFlags.None));
        Assert.That(loaded.Levels["level-04"].Completed, Is.False);
        Assert.That(loaded.CompletedLevels, Is.Null);
        Assert.That(loaded.BestStars, Is.Null);
    }

    [Test]
    public void MigratedSave_IsWrittenAsTheCurrentVersionWithoutLegacyFields()
    {
        Write("progress.json", V1_JSON);

        var service = CreateService();

        service.Settle(new LevelId("level-04"), LevelResults.Win(StarFlags.Goal));

        var json = File.ReadAllText(Path.Combine(_directory, "progress.json"));

        Assert.That(json, Does.Contain("\"Version\":4"));
        Assert.That(json, Does.Not.Contain("BestStars"));
        Assert.That(json, Does.Not.Contain("CompletedLevels"));
        Assert.That(json, Does.Not.Contain("Coins"));
    }

    [Test]
    public void VersionThreeSave_GrantsTheGiftForBoosterUnlocksAlreadyGranted()
    {
        Write("progress.json", V3_BOOSTER_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Version, Is.EqualTo(ProgressSave.CURRENT_VERSION));
        Assert.That(loaded.BoosterStock["ExtraTime"], Is.EqualTo(3));
        Assert.That(loaded.BoosterStock["Turbo"], Is.EqualTo(3));
        Assert.That(loaded.EquippedBoosters, Is.Empty);
    }

    [Test]
    public void VersionThreeSave_WithoutBoosterUnlocks_StartsWithNoStock()
    {
        Write("progress.json", V3_NO_BOOSTER_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Version, Is.EqualTo(ProgressSave.CURRENT_VERSION));
        Assert.That(loaded.BoosterStock, Is.Empty);
    }

    [Test]
    public void BoosterStockAndEquipment_SurviveSaveAndReload()
    {
        var service = CreateService();
        var unlock = new UnlockSettings { Kind = UnlockKind.TurboBooster };

        service.Settle(new LevelId("level-06"), LevelResults.Win(StarFlags.Goal), unlock);
        service.SetBoosterEquipped(BoosterKind.Turbo, isEquipped: true);

        var reloaded = CreateService();

        Assert.That(reloaded.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(3));
        Assert.That(reloaded.IsBoosterEquipped(BoosterKind.Turbo), Is.True);
        Assert.That(reloaded.GetBoosterStock(BoosterKind.ExtraTime), Is.Zero);
    }

    [Test]
    public void VersionTwoSave_MigratesToOwnTheStandardMachine()
    {
        Write("progress.json", VALID_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Version, Is.EqualTo(ProgressSave.CURRENT_VERSION));
        Assert.That(loaded.OwnedMachines, Is.EquivalentTo(new[] { MachineIds.Standard.Value }));
        Assert.That(loaded.SelectedMachine, Is.EqualTo(MachineIds.Standard.Value));
        Assert.That(loaded.GrantedUnlocks, Is.Empty);
        Assert.That(loaded.Levels["level-01"].Attempts, Is.EqualTo(40));
    }

    [Test]
    public void VersionOneSave_MigratesToOwnTheStandardMachine()
    {
        Write("progress.json", V1_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.OwnedMachines, Is.EquivalentTo(new[] { MachineIds.Standard.Value }));
        Assert.That(loaded.SelectedMachine, Is.EqualTo(MachineIds.Standard.Value));
    }

    [Test]
    public void MachinesAndUnlocks_RoundTrip()
    {
        var store = new FileProgressStore(_directory);
        var save = ProgressSave.CreateNew();

        save.OwnedMachines.Add(MachineIds.Wide.Value);
        save.SelectedMachine = MachineIds.Wide.Value;
        save.GrantedUnlocks.Add("level-10:WideMachine");

        Assert.That(store.Save(save).IsSuccess, Is.True);
        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.OwnedMachines, Is.EquivalentTo(new[] { MachineIds.Standard.Value, MachineIds.Wide.Value }));
        Assert.That(loaded.SelectedMachine, Is.EqualTo(MachineIds.Wide.Value));
        Assert.That(loaded.GrantedUnlocks, Is.EquivalentTo(new[] { "level-10:WideMachine" }));
    }

    [Test]
    public void VersionOneSaveWithoutAnyLevels_MigratesToAnEmptyVersionTwoSave()
    {
        Write("progress.json", "{\"Version\":1,\"Coins\":12}");

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Version, Is.EqualTo(ProgressSave.CURRENT_VERSION));
        Assert.That(loaded.Levels, Is.Empty);
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
    public void RestartOnTheSameResult_ReportsNoNewStars()
    {
        var first = CreateService();

        first.Settle(s_level, LevelResults.Win(StarRules.ALL));

        var restarted = CreateService();

        Assert.That(restarted.GetStars(s_level), Is.EqualTo(StarRules.ALL));
        Assert.That(restarted.Settle(s_level, LevelResults.Win(StarRules.ALL)).TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.New, Is.EqualTo(StarFlags.None));
        Assert.That(settlement.IsFirstCompletion, Is.False);
        Assert.That(restarted.GetAttempts(s_level), Is.EqualTo(2));
    }

    [Test]
    public void ReloadedSave_KeepsTheMergedStars()
    {
        var service = CreateService();

        service.Settle(s_level, LevelResults.Win(StarFlags.Goal | StarFlags.Clean));
        service.Settle(s_level, LevelResults.Win(StarFlags.Goal | StarFlags.Side));
        service.Reload();

        Assert.That(service.GetStars(s_level), Is.EqualTo(StarRules.ALL));
        Assert.That(service.GetAttempts(s_level), Is.EqualTo(2));
    }

    [Test]
    public void OnlyTempFilePresent_Recovers()
    {
        Write("progress.json.tmp", VALID_JSON);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Levels[s_level.Value].Attempts, Is.EqualTo(40));
    }

    [Test]
    public void GarbageTempFileWithValidMain_LoadsMain()
    {
        Write("progress.json", VALID_JSON);
        Write("progress.json.tmp", GARBAGE);

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Levels[s_level.Value].Attempts, Is.EqualTo(40));
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
        Assert.That(loaded.Levels[s_level.Value].Attempts, Is.EqualTo(7));
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
    public void NegativeAttempts_AreRejected()
    {
        Write("progress.json", "{\"Version\":2,\"Levels\":{\"level-01\":{\"Attempts\":-5}}}");

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

        save.Levels[s_level.Value] = new LevelRecord { Attempts = 125 };

        Assert.That(store.Save(save).IsSuccess, Is.True);

        Assert.That(ReadSave("progress.json").Levels[s_level.Value].Attempts, Is.EqualTo(125));
        Assert.That(ReadSave("progress.json.bak").Levels[s_level.Value].Attempts, Is.EqualTo(7));

        var quarantined = Directory.GetFiles(_directory, "progress.json.corrupt-*");

        Assert.That(quarantined.Length, Is.EqualTo(1));
        Assert.That(File.ReadAllText(quarantined[0]), Is.EqualTo(GARBAGE));
    }

    [Test]
    public void RepeatedSaves_KeepTheLatestInMainAndThePreviousInBackup()
    {
        var store = new FileProgressStore(_directory);

        for (var attempts = 10; attempts <= 40; attempts += 10)
        {
            var save = ProgressSave.CreateNew();

            save.Levels[s_level.Value] = new LevelRecord { Attempts = attempts };

            Assert.That(store.Save(save).IsSuccess, Is.True);
        }

        Assert.That(ReadSave("progress.json").Levels[s_level.Value].Attempts, Is.EqualTo(40));
        Assert.That(ReadSave("progress.json.bak").Levels[s_level.Value].Attempts, Is.EqualTo(30));
        Assert.That(File.Exists(Path.Combine(_directory, "progress.json.tmp")), Is.False);
        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Levels[s_level.Value].Attempts, Is.EqualTo(40));
    }

    [Test]
    public void MainMissing_LoadsTheNewestRevisionOfBackupAndTemp()
    {
        Write("progress.json.bak", Json(revision: 3, attempts: 7));
        Write("progress.json.tmp", Json(revision: 4, attempts: 40));

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Levels[s_level.Value].Attempts, Is.EqualTo(40));
    }

    [Test]
    public void MainMissing_TempWithAnOlderRevisionLosesToTheBackup()
    {
        Write("progress.json.bak", Json(revision: 5, attempts: 7));
        Write("progress.json.tmp", Json(revision: 4, attempts: 40));

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Levels[s_level.Value].Attempts, Is.EqualTo(7));
    }

    [Test]
    public void MainMissing_EqualRevisionsPreferTheTemp()
    {
        Write("progress.json.bak", Json(revision: 2, attempts: 7));
        Write("progress.json.tmp", Json(revision: 2, attempts: 40));

        var store = new FileProgressStore(_directory);

        Assert.That(store.Load().TryGetValue(out var loaded), Is.True);
        Assert.That(loaded.Levels[s_level.Value].Attempts, Is.EqualTo(40));
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

            var outcome = service.Settle(s_level, LevelResults.Win(StarRules.ALL));

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

        service.Settle(s_level, LevelResults.Win(StarFlags.Goal));
        service.Settle(s_level, LevelResults.Win(StarRules.ALL));

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

    private static string Json(int revision, int attempts)
    {
        return $"{{\"Version\":2,\"Revision\":{revision},"
            + $"\"Levels\":{{\"level-01\":{{\"Attempts\":{attempts}}}}}}}";
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
