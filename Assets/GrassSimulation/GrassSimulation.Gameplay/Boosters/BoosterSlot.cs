namespace GrassSimulation.Gameplay
{
    public readonly record struct BoosterSlot(
          BoosterSlotState State
        , bool IsEquipped
        , bool IsAllowed
        , int Stock
        , float Remaining
        , float Duration
    );
}
