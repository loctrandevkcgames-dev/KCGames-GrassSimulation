namespace GrassSimulation.Gameplay
{
    public readonly record struct LevelIssue(LevelRule Rule, string Message)
    {
        public override string ToString()
            => $"[{Rule}] {Message}";
    }
}
