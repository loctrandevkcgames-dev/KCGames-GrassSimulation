using EncosyTower.Common;
using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public readonly record struct ResultBonus(Option<QuotaSnapshot> First, bool AreAllMet);
}
