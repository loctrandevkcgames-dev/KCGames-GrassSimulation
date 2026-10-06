namespace GrassSimulation.Gameplay
{
    public interface ILevelFlowHost
    {
        LevelSession Session { get; }

        void Retry();

        void LoadNext();

        void GoHome();

        void Play();

        void FinishCleanup();

        void RetrySave();
    }
}
