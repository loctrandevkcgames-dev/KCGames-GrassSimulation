using EncosyTower.CodeGen;
using EncosyTower.PubSub;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(CameraScope))]
    public readonly partial record struct PreviewViewportChangedMsg(Rect Viewport);
}
