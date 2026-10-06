using System.Buffers;
using System.Collections.Generic;
using EncosyTower.PubSub;
using EncosyTower.Tasks;
using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelCommandRouterTests
{
    private const int CUTTABLE_CELLS = 100;

    private TestAssets _assets;
    private MessageRecorder _messages;
    private Messenger _commandMessenger;
    private FakeFlowHost _host;
    private LevelCommandRouter _router;
    private MessagePublisher.Publisher<LevelCommandScope> _commands;

    [SetUp]
    public void SetUp()
    {
        _assets = new TestAssets();
        _messages = new MessageRecorder();
        _commandMessenger = new Messenger(ArrayPool<UnityTask>.Shared);

        var level = _assets.CreateLevel(
              timeLimit: 10f
            , failOnProtectedHits: false
            , new QuotaSettings { Kind = PlantKind.Grass, Amount = 50 }
        );

        var session = new LevelSession(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher);

        _host = new FakeFlowHost(session);
        _commands = _commandMessenger.Publisher.Scope<LevelCommandScope>();
        _router = new LevelCommandRouter(_commandMessenger.Subscriber.Scope<LevelCommandScope>(), _host);
    }

    [TearDown]
    public void TearDown()
    {
        _router.Dispose();
        _commandMessenger.Dispose();
        _messages.Dispose();
        _assets.Dispose();
    }

    [Test]
    public void StartRequested_BeginsThePreviewSession()
    {
        StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());

        Assert.That(_host.Session.State, Is.EqualTo(LevelState.Playing));
    }

    [Test]
    public void UpgradeRequested_WithAnInvalidOptionOrNoPendingUpgrade_IsIgnored()
    {
        StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
        UpgradeRequestedMsg.Publish(in _commands, new UpgradeRequestedMsg(Option: 0));
        _host.Session.RecordHarvest(PlantKind.Grass, xp: 100);
        _host.Session.EndTick(0.01f);

        UpgradeRequestedMsg.Publish(in _commands, new UpgradeRequestedMsg(Option: 5));
        UpgradeRequestedMsg.Publish(in _commands, new UpgradeRequestedMsg(Option: -1));

        Assert.That(_host.Session.State, Is.EqualTo(LevelState.UpgradeChoice));
        Assert.That(_messages.UpgradesChosen, Is.Empty);

        UpgradeRequestedMsg.Publish(in _commands, new UpgradeRequestedMsg(Option: 1));

        Assert.That(_host.Session.State, Is.EqualTo(LevelState.Playing));
        Assert.That(_messages.UpgradesChosen, Has.Count.EqualTo(1));
    }

    [Test]
    public void PauseRequested_PausesAndResumes()
    {
        StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());

        PauseRequestedMsg.Publish(in _commands, new PauseRequestedMsg(Paused: true));
        Assert.That(_host.Session.IsPaused, Is.True);

        PauseRequestedMsg.Publish(in _commands, new PauseRequestedMsg(Paused: false));
        Assert.That(_host.Session.IsPaused, Is.False);
    }

    [Test]
    public void PauseRequested_IsIgnoredInPreviewAndUpgradeChoice()
    {
        PauseRequestedMsg.Publish(in _commands, new PauseRequestedMsg(Paused: true));
        Assert.That(_host.Session.IsPaused, Is.False);

        StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
        _host.Session.RecordHarvest(PlantKind.Grass, xp: 100);
        _host.Session.EndTick(0.01f);
        PauseRequestedMsg.Publish(in _commands, new PauseRequestedMsg(Paused: true));

        Assert.That(_host.Session.State, Is.EqualTo(LevelState.UpgradeChoice));
        Assert.That(_host.Session.IsPaused, Is.False);
    }

    [Test]
    public void FlowCommandIssuedInsideLevelFinished_IsLeftToTheHostSoTheLastStateIsTheNewSessions()
    {
        var subscriber = _messages.Subscriber;

        LevelFinishedMsg.Subscribe(
              in subscriber
            , (LevelFinishedMsg _) => RetryRequestedMsg.Publish(in _commands, new RetryRequestedMsg())
        );
        StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
        _host.Session.EndTick(100f);

        Assert.That(_host.Calls, Is.EqualTo(new[] { "Retry" }));
        var failed = new LevelStateChangedMsg(State: LevelState.Failure, IsPaused: false);

        Assert.That(_messages.StateChanges[^1], Is.EqualTo(failed));

        _host.RunPending();

        var preview = new LevelStateChangedMsg(State: LevelState.Preview, IsPaused: false);

        Assert.That(_messages.StateChanges[^1], Is.EqualTo(preview));
    }

    [Test]
    public void CleanupRequested_OnlyWorksAfterSuccess()
    {
        StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
        CleanupRequestedMsg.Publish(in _commands, new CleanupRequestedMsg());

        Assert.That(_host.Session.State, Is.EqualTo(LevelState.Playing));
    }

    [Test]
    public void FlowCommands_ReachTheHost()
    {
        RetryRequestedMsg.Publish(in _commands, new RetryRequestedMsg());
        NextLevelRequestedMsg.Publish(in _commands, new NextLevelRequestedMsg());
        FinishCleanupRequestedMsg.Publish(in _commands, new FinishCleanupRequestedMsg());
        QuitRequestedMsg.Publish(in _commands, new QuitRequestedMsg());
        PlayRequestedMsg.Publish(in _commands, new PlayRequestedMsg());
        RetrySaveRequestedMsg.Publish(in _commands, new RetrySaveRequestedMsg());

        Assert.That(
              _host.Calls
            , Is.EqualTo(new[] { "Retry", "LoadNext", "FinishCleanup", "GoHome", "Play", "RetrySave" })
        );
    }

    [Test]
    public void Dispose_StopsRoutingCommands()
    {
        _router.Dispose();

        StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
        RetryRequestedMsg.Publish(in _commands, new RetryRequestedMsg());

        Assert.That(_host.Session.State, Is.EqualTo(LevelState.Preview));
        Assert.That(_host.Calls, Is.Empty);
    }

    private sealed class FakeFlowHost : ILevelFlowHost
    {
        private bool _hasPendingReset;

        public FakeFlowHost(LevelSession session)
        {
            Session = session;
        }

        public LevelSession Session { get; }

        public List<string> Calls { get; } = new();

        public void Retry()
        {
            Calls.Add(nameof(Retry));
            _hasPendingReset = true;
        }

        public void RunPending()
        {
            if (_hasPendingReset)
            {
                _hasPendingReset = false;
                Session.Reset();
            }
        }

        public void LoadNext() => Calls.Add(nameof(LoadNext));

        public void GoHome() => Calls.Add(nameof(GoHome));

        public void Play() => Calls.Add(nameof(Play));

        public void FinishCleanup() => Calls.Add(nameof(FinishCleanup));

        public void RetrySave() => Calls.Add(nameof(RetrySave));
    }
}
