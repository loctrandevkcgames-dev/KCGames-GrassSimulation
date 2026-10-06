namespace GrassSimulation.Gameplay
{
    public readonly record struct QuotaSnapshot(PlantKind Kind, int Amount, int Progress, bool IsBonus, bool IsMet);
}
