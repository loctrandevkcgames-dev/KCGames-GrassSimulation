using EncosyTower.Mvvm.ComponentModel;
using UnityEngine;

namespace GrassSimulation.UI
{
    [ObservableObject]
    public sealed partial class GameplayViewModel : MonoBehaviour
    {
        [ObservableProperty]
        public string TimerText { get => Get_TimerText(); set => Set_TimerText(value); }
    }
}
