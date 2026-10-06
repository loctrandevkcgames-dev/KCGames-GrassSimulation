using System.Buffers;
using EncosyTower.Common;
using EncosyTower.PubSub;
using EncosyTower.Tasks;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class LevelResultCacheTests
{
    private static readonly LevelId s_level = new("level-01");

    private Messenger _messenger;
    private LevelResultCache _cache;
    private MessagePublisher.Publisher<GameplayScope> _gameplay;
    private MessagePublisher.Publisher<ProgressionScope> _progression;

    [SetUp]
    public void SetUp()
    {
        _messenger = new Messenger(ArrayPool<UnityTask>.Shared);
        _gameplay = _messenger.Publisher.Scope<GameplayScope>();
        _progression = _messenger.Publisher.Scope<ProgressionScope>();

        _cache = new LevelResultCache(
              _messenger.Subscriber.Scope<GameplayScope>()
            , _messenger.Subscriber.Scope<ProgressionScope>()
        );
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
        _messenger.Dispose();
    }

    [Test]
    public void GetLast_IsEmptyAtFirst()
    {
        var last = _cache.GetLast();

        Assert.That(last.Version, Is.Zero);
        Assert.That(last.Finished.HasValue, Is.False);
        Assert.That(last.Settlement.HasValue, Is.False);
    }

    [Test]
    public void LevelFinished_StoresTheResultAndBumpsTheVersion()
    {
        PublishFinished();

        var last = _cache.GetLast();

        Assert.That(last.Version, Is.EqualTo(1));
        Assert.That(last.Finished.TryGetValue(out var result), Is.True);
        Assert.That(result.Outcome.IsSuccess, Is.True);
        Assert.That(last.Settlement.HasValue, Is.False);
    }

    [Test]
    public void LevelSettled_BeforeFinished_IsKeptAlongsideTheResult()
    {
        PublishSettled();
        PublishFinished();

        var last = _cache.GetLast();

        Assert.That(last.Version, Is.EqualTo(2));
        Assert.That(last.Finished.HasValue, Is.True);
        Assert.That(last.Settlement.TryGetValue(out var outcome), Is.True);
        Assert.That(outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.CoinsGranted, Is.EqualTo(150));
    }

    [TestCase(LevelState.Preview)]
    [TestCase(LevelState.Playing)]
    public void RestartingStates_ClearTheCache(LevelState state)
    {
        PublishFinished();
        PublishSettled();

        var before = _cache.GetLast().Version;

        PublishState(state);

        var last = _cache.GetLast();

        Assert.That(last.Finished.HasValue, Is.False);
        Assert.That(last.Settlement.HasValue, Is.False);
        Assert.That(last.Version, Is.EqualTo(before + 1));
    }

    [TestCase(LevelState.Success)]
    [TestCase(LevelState.Failure)]
    [TestCase(LevelState.Cleanup)]
    [TestCase(LevelState.UpgradeChoice)]
    public void OtherStates_KeepTheCache(LevelState state)
    {
        PublishFinished();

        var before = _cache.GetLast().Version;

        PublishState(state);

        var last = _cache.GetLast();

        Assert.That(last.Finished.HasValue, Is.True);
        Assert.That(last.Version, Is.EqualTo(before));
    }

    [Test]
    public void ClearingAnEmptyCache_DoesNotBumpTheVersion()
    {
        PublishState(LevelState.Playing);

        Assert.That(_cache.GetLast().Version, Is.Zero);
    }

    [Test]
    public void Dispose_StopsListening()
    {
        _cache.Dispose();

        PublishFinished();

        Assert.That(_cache.GetLast().Finished.HasValue, Is.False);
    }

    private void PublishFinished()
    {
        LevelOutcome outcome = new LevelOutcome.Success(Stars: 2);
        var result = new LevelResult(Outcome: outcome, RemainingTime: 40f, ProtectedHits: 0);

        LevelFinishedMsg.Publish(in _gameplay, new LevelFinishedMsg(Level: s_level, Result: result, Duration: 20f));
    }

    private void PublishSettled()
    {
        var settlement = new LevelSettlement(
              CoinsGranted: 150
            , FirstWinCoins: 100
            , NewStars: 2
            , Stars: 2
            , IsNewBest: true
            , IsFirstCompletion: true
        );

        var outcome = Result<LevelSettlement, SettleError>.Succeed(settlement);

        LevelSettledMsg.Publish(in _progression, new LevelSettledMsg(Level: s_level, Outcome: outcome));
    }

    private void PublishState(LevelState state)
    {
        LevelStateChangedMsg.Publish(in _gameplay, new LevelStateChangedMsg(State: state, IsPaused: false));
    }
}
