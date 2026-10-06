using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelStateChangedTests
{
    private const int CUTTABLE_CELLS = 100;

    private TestAssets _assets;
    private MessageRecorder _messages;

    [SetUp]
    public void SetUp()
    {
        _assets = new TestAssets();
        _messages = new MessageRecorder();
    }

    [TearDown]
    public void TearDown()
    {
        _messages.Dispose();
        _assets.Dispose();
    }

    [Test]
    public void Construction_DoesNotPublish()
    {
        CreateSession(Quota(PlantKind.Grass, 5));

        Assert.That(_messages.StateChanges, Is.Empty);
    }

    [Test]
    public void Reset_PublishesPreview()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 5));

        session.Reset();

        var expected = new[] { new LevelStateChangedMsg(State: LevelState.Preview, IsPaused: false) };

        Assert.That(_messages.StateChanges, Is.EqualTo(expected));
    }

    [Test]
    public void Begin_PublishesPlayingOnce()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 5));

        session.TryBegin();
        session.TryBegin();

        var expected = new[] { new LevelStateChangedMsg(State: LevelState.Playing, IsPaused: false) };

        Assert.That(_messages.StateChanges, Is.EqualTo(expected));
    }

    [Test]
    public void PauseAndResume_PublishOnlyWhenTheValueChanges()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 5));
        session.Pause();
        session.Resume();
        Assert.That(_messages.StateChanges, Is.Empty);

        session.TryBegin();
        _messages.StateChanges.Clear();

        session.Pause();
        session.Pause();
        session.Resume();
        session.Resume();

        Assert.That(
              _messages.StateChanges
            , Is.EqualTo(
                new[] {
                    new LevelStateChangedMsg(State: LevelState.Playing, IsPaused: true),
                    new LevelStateChangedMsg(State: LevelState.Playing, IsPaused: false),
                }
            )
        );
    }

    [Test]
    public void Upgrade_PublishesChoiceThenPlayingOnlyWhenNoUpgradeIsPending()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 50));
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 300);
        _messages.StateChanges.Clear();

        session.EndTick(0.01f);
        session.EndTick(0.01f);
        session.TryChooseUpgrade(0);
        session.TryChooseUpgrade(7);
        session.TryChooseUpgrade(1);

        Assert.That(
              _messages.StateChanges
            , Is.EqualTo(
                new[] {
                    new LevelStateChangedMsg(State: LevelState.UpgradeChoice, IsPaused: false),
                    new LevelStateChangedMsg(State: LevelState.Playing, IsPaused: false),
                }
            )
        );
    }

    [Test]
    public void Success_ThenCleanup_PublishesEachState()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 1));
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        _messages.StateChanges.Clear();

        session.EndTick(0.1f);
        session.TryEnterCleanup();
        session.TryEnterCleanup();

        Assert.That(
              _messages.StateChanges
            , Is.EqualTo(
                new[] {
                    new LevelStateChangedMsg(State: LevelState.Success, IsPaused: false),
                    new LevelStateChangedMsg(State: LevelState.Cleanup, IsPaused: false),
                }
            )
        );
    }

    [Test]
    public void Failure_PublishesFailure()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 5));
        session.TryBegin();
        _messages.StateChanges.Clear();

        session.EndTick(100f);

        var expected = new[] { new LevelStateChangedMsg(State: LevelState.Failure, IsPaused: false) };

        Assert.That(_messages.StateChanges, Is.EqualTo(expected));
    }

    [Test]
    public void State_IsAlreadyMutatedWhenThePauseMessageArrives()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 5));
        var subscriber = _messages.Subscriber;
        var seenPaused = false;

        LevelStateChangedMsg.Subscribe(in subscriber, (LevelStateChangedMsg _) => seenPaused = session.IsPaused);
        session.TryBegin();
        session.Pause();

        Assert.That(seenPaused, Is.True);
    }

    [Test]
    public void Finish_PublishesLevelFinishedBeforeTheTerminalState()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 5));
        var subscriber = _messages.Subscriber;
        var order = new System.Collections.Generic.List<string>();

        session.TryBegin();

        LevelFinishedMsg.Subscribe(in subscriber, (LevelFinishedMsg _) => order.Add("finished"));
        LevelStateChangedMsg.Subscribe(
              in subscriber
            , (LevelStateChangedMsg message) => order.Add(message.State.ToStringFast())
        );

        session.EndTick(100f);

        Assert.That(order, Is.EqualTo(new[] { "finished", "Failure" }));
    }

    [Test]
    public void PausingInsideUpgradeChosen_DoesNotDuplicateTheMessages()
    {
        var session = CreateSession(Quota(PlantKind.Grass, 50));
        var subscriber = _messages.Subscriber;

        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 100);
        session.EndTick(0.01f);
        _messages.StateChanges.Clear();

        UpgradeChosenMsg.Subscribe(in subscriber, (UpgradeChosenMsg _) => session.Pause());
        session.TryChooseUpgrade(0);

        Assert.That(
              _messages.StateChanges
            , Is.EqualTo(
                new[] {
                    new LevelStateChangedMsg(State: LevelState.Playing, IsPaused: false),
                    new LevelStateChangedMsg(State: LevelState.Playing, IsPaused: true),
                }
            )
        );
    }

    private static QuotaSettings Quota(PlantKind kind, int amount, bool isBonus = false)
        => new() { Kind = kind, Amount = amount, IsBonus = isBonus };

    private LevelSession CreateSession(params QuotaSettings[] quotas)
    {
        var level = _assets.CreateLevel(timeLimit: 10f, quotas);
        return new LevelSession(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher);
    }
}
