using EncosyTower.CodeGen;
using EncosyTower.PubSub;

namespace GrassSimulation.Gameplay
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(GameplayScope))]
    public readonly partial record struct PlantHarvestedMsg(PlantKind Kind, int Xp, int FruitCount, bool IsFruit);
}
