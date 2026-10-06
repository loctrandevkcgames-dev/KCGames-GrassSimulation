using EncosyTower.PageFlows;
using UnityEngine.Scripting;

namespace GrassSimulation.UI
{
    [Preserve]
    public struct GrassPageFlowScopes : IPageFlowScopeCollection
    {
        [Preserve]
        public PageFlowScope Screen { get; set; }

        [Preserve]
        public PageFlowScope Popup { get; set; }
    }
}
