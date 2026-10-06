using EncosyTower.Common;
using UnityEngine;

namespace GrassSimulation.UI
{
    public readonly record struct ResultRowData(string Label, Option<string> Value, Color Tone, bool ShowCheck);
}
