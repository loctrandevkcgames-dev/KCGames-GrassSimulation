using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class OnboardingHintModelTests
{
    private const float FRAME = 0.5f;

    private static bool Tick(
          OnboardingHintModel model
        , bool isActive = false
        , int levelIndex = 0
        , LevelState state = LevelState.Playing
        , bool isPaused = false
    )
    {
        return model.Update(FRAME, levelIndex, state, isPaused, isActive);
    }

    private static void Idle(OnboardingHintModel model, float seconds)
    {
        for (var elapsed = 0f; elapsed < seconds; elapsed += FRAME)
        {
            Tick(model);
        }
    }

    [Test]
    public void IsVisibleFromTheStartOfTheFirstLevel()
    {
        var model = new OnboardingHintModel();

        Assert.That(Tick(model), Is.True);
        Assert.That(model.IsVisible, Is.True);
    }

    [Test]
    public void StaysVisibleWhileTheMowerHasNeverMoved()
    {
        var model = new OnboardingHintModel();

        Idle(model, 60f);

        Assert.That(model.IsVisible, Is.True);
    }

    [Test]
    public void HidesWhenThePlayerFirstMoves()
    {
        var model = new OnboardingHintModel();

        Tick(model);

        Assert.That(Tick(model, isActive: true), Is.False);
    }

    [Test]
    public void StaysHiddenForShortIdleAfterMoving()
    {
        var model = new OnboardingHintModel();

        Tick(model, isActive: true);
        Idle(model, OnboardingHintModel.IDLE_SECONDS - 1f);

        Assert.That(model.IsVisible, Is.False);
    }

    [Test]
    public void ReturnsAfterEightSecondsIdle()
    {
        var model = new OnboardingHintModel();

        Tick(model, isActive: true);
        Idle(model, OnboardingHintModel.IDLE_SECONDS);

        Assert.That(model.IsVisible, Is.True);
    }

    [Test]
    public void HidesAgainWhenMovingResumes()
    {
        var model = new OnboardingHintModel();

        Tick(model, isActive: true);
        Idle(model, OnboardingHintModel.IDLE_SECONDS);

        Assert.That(Tick(model, isActive: true), Is.False);
    }

    [Test]
    public void MovingResetsTheIdleTimer()
    {
        var model = new OnboardingHintModel();

        Tick(model, isActive: true);
        Idle(model, OnboardingHintModel.IDLE_SECONDS - 1f);
        Tick(model, isActive: true);
        Idle(model, OnboardingHintModel.IDLE_SECONDS - 1f);

        Assert.That(model.IsVisible, Is.False);
    }

    [Test]
    public void NeverShowsOnLaterLevels()
    {
        var model = new OnboardingHintModel();

        Idle(model, 30f);

        Assert.That(Tick(model, levelIndex: 1), Is.False);
    }

    [TestCase(LevelState.UpgradeChoice)]
    [TestCase(LevelState.Success)]
    [TestCase(LevelState.Failure)]
    [TestCase(LevelState.Cleanup)]
    public void HiddenOutsidePlaying(LevelState state)
    {
        var model = new OnboardingHintModel();

        Assert.That(Tick(model, state: state), Is.False);
    }

    [Test]
    public void HiddenWhilePausedAndDoesNotCountPausedTime()
    {
        var model = new OnboardingHintModel();

        Tick(model, isActive: true);

        for (var i = 0; i < 40; i++)
        {
            Assert.That(Tick(model, isPaused: true), Is.False);
        }

        Assert.That(Tick(model), Is.False);
    }

    [Test]
    public void ResumesAfterPopupWithoutLosingMovedState()
    {
        var model = new OnboardingHintModel();

        Tick(model, isActive: true);
        Tick(model, state: LevelState.UpgradeChoice);

        Assert.That(Tick(model), Is.False);
    }

    [Test]
    public void PreviewStateResetsForTheNextRun()
    {
        var model = new OnboardingHintModel();

        Tick(model, isActive: true);
        Tick(model, state: LevelState.Preview);

        Assert.That(model.IsVisible, Is.False);
        Assert.That(Tick(model), Is.True);
    }

    [Test]
    public void ResetClearsMovedStateAndVisibility()
    {
        var model = new OnboardingHintModel();

        Tick(model, isActive: true);
        model.Reset();

        Assert.That(model.IsVisible, Is.False);
        Assert.That(Tick(model), Is.True);
    }
}
