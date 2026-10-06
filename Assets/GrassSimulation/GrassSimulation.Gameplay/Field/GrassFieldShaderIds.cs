using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public static class GrassFieldShaderIds
    {
        public static readonly int CellState = Shader.PropertyToID("_GrassCellState");
        public static readonly int Litter = Shader.PropertyToID("_GrassLitter");
        public static readonly int LitterAccent = Shader.PropertyToID("_GrassLitterAccent");
        public static readonly int CutState = Shader.PropertyToID("_GrassCutState");
        public static readonly int Sweep = Shader.PropertyToID("_GrassSweep");
        public static readonly int FieldParams = Shader.PropertyToID("_GrassFieldParams");
        public static readonly int BladeParams = Shader.PropertyToID("_GrassBladeParams");
        public static readonly int BladeState = Shader.PropertyToID("_GrassBladeState");
        public static readonly int Flash = Shader.PropertyToID("_Flash");
        public static readonly int TipColor = Shader.PropertyToID("_TipColor");
    }
}
