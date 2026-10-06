using System.Collections.Generic;
using EncosyTower.Processing;
using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.Progression.Tests;

public sealed class ProgressSnapshotTests
{
    private FakeProgressStore _store;
    private ProgressionService _service;

    [SetUp]
    public void SetUp()
    {
        _store = new FakeProgressStore();
        _service = new ProgressionService(_store);
        _service.Initialize();
    }

    [Test]
    public void From_ReportsAFreshSave()
    {
        var catalog = TestCatalogs.Create("level-01", "level-02", "level-03", out var objects);

        try
        {
            var snapshot = ProgressSnapshot.From(_service, catalog, hasPendingSave: false);

            Assert.That(snapshot, Is.EqualTo(new ProgressSnapshot(
                      CompletedCount: 0
                    , LevelCount: 3
                    , NextLevelIndex: 0
                    , TotalStars: 0
                    , IsReadOnly: false
                    , HasPendingSave: false
                )));
        }
        finally
        {
            TestCatalogs.DestroyAll(objects);
        }
    }

    [Test]
    public void From_CountsCompletedLevelsStarsAndNextLevel()
    {
        var catalog = TestCatalogs.Create("level-01", "level-02", "level-03", out var objects);

        try
        {
            _service.Settle(new LevelId("level-01"), LevelResults.Win(StarFlags.Goal | StarFlags.Clean));
            _service.Settle(new LevelId("level-03"), LevelResults.Win(StarRules.ALL));

            var snapshot = ProgressSnapshot.From(_service, catalog, hasPendingSave: true);

            Assert.That(snapshot.CompletedCount, Is.EqualTo(2));
            Assert.That(snapshot.LevelCount, Is.EqualTo(3));
            Assert.That(snapshot.NextLevelIndex, Is.EqualTo(1));
            Assert.That(snapshot.TotalStars, Is.EqualTo(5));
            Assert.That(snapshot.IsReadOnly, Is.False);
            Assert.That(snapshot.HasPendingSave, Is.True);
        }
        finally
        {
            TestCatalogs.DestroyAll(objects);
        }
    }

    [Test]
    public void From_ReportsAReadOnlySave()
    {
        var catalog = TestCatalogs.Create("level-01", "level-02", "level-03", out var objects);

        try
        {
            _store.LoadFailure = new LoadError.Unreadable("fake", "broken");
            _service.Reload();

            var snapshot = ProgressSnapshot.From(_service, catalog, hasPendingSave: false);

            Assert.That(snapshot.IsReadOnly, Is.True);
        }
        finally
        {
            TestCatalogs.DestroyAll(objects);
        }
    }

    [Test]
    public void Request_ReturnsTheSnapshotThroughTheScopedHub()
    {
        var catalog = TestCatalogs.Create("level-01", "level-02", "level-03", out var objects);
        var registries = new List<ProcessRegistry>();

        try
        {
            using var processor = new Processor();

            var registration = processor.Scope<ProgressionScope>().WithRegistries(registries);

            GetProgressSnapshotRequest.Register(
                  in registration
                , (GetProgressSnapshotRequest _) => ProgressSnapshot.From(_service, catalog, hasPendingSave: false)
            );

            var hub = processor.Scope<ProgressionScope>();
            var result = GetProgressSnapshotRequest.TryProcess(in hub, new GetProgressSnapshotRequest());

            Assert.That(result.TryGetValue(out var snapshot), Is.True);
            Assert.That(snapshot.LevelCount, Is.EqualTo(3));

            registries.Unregister();
        }
        finally
        {
            TestCatalogs.DestroyAll(objects);
        }
    }
}
