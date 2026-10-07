namespace GrassSimulation.Gameplay
{
    public interface ILevelFlowHost
    {
        LevelSession Session { get; }

        void Begin();

        void Retry();

        void LoadNext();

        void GoHome();

        void Play();

        void FinishCleanup();

        void RetrySave();

        void SelectMachine(MachineId machine);

        void BackToPreview();

        void ChangeMachine();

        void SetBoosterEquipped(BoosterKind kind, bool isEquipped);
    }
}
