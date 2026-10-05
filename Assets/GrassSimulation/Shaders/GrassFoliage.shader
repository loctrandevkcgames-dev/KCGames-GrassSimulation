// Plants of the Grass Route field: grass, thick grass, harvest flowers, bushes and protected flowers.
// Use one material per PlantDefinition. The mesh data contract is documented in Include/GrassFoliage.hlsl and the
// cell state contract in Include/GrassField.hlsl.
Shader "GrassSimulation/Foliage"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("Blade Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Root Color", Color) = (0.20, 0.45, 0.12, 1)
        _TipColor ("Tip Color", Color) = (0.55, 0.85, 0.30, 1)
        _RootOcclusion ("Root Occlusion", Range(0, 1)) = 0.45
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Color Patches)]
        _PatchColor ("Patch Tint (A = amount)", Color) = (1.15, 1.05, 0.55, 0.5)
        _PatchScale ("Patch Scale (1/m)", Float) = 0.35

        [Header(Cartoon Lighting)]
        _ShadeColor ("Shade Color", Color) = (0.32, 0.45, 0.62, 1)
        _ShadeThreshold ("Shade Threshold", Range(0, 1)) = 0.45
        _ShadeSoftness ("Shade Softness", Range(0.001, 0.5)) = 0.06
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.85
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.25
        _TipHighlight ("Tip Highlight (A = amount)", Color) = (1, 1, 0.75, 0.25)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.2
        _NormalUpBlend ("Normal Up Blend", Range(0, 1)) = 0.85
        _Translucency ("Translucency", Range(0, 2)) = 0.3

        [Header(Wind)]
        _WindDirection ("Wind Direction (XZ)", Vector) = (1, 0, 0.3, 0)
        _WindStrength ("Wind Lean (m)", Range(0, 0.5)) = 0.06
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.2
        _WindFrequency ("Wind Wave Frequency", Range(0, 5)) = 0.8
        _WindFlutter ("Wind Flutter (m)", Range(0, 0.2)) = 0.015
        _WindHighlight ("Wind Wave Highlight (A = amount)", Color) = (1, 1, 0.8, 0.3)

        [Header(Blade Interaction)]
        _ShakeAmplitude ("Contact Shake (m)", Range(0, 0.3)) = 0.08
        _ShakeFrequency ("Contact Shake Frequency", Range(0, 80)) = 45
        _PushStrength ("Blade Push (m)", Range(0, 0.6)) = 0.18
        _PushMargin ("Blade Push Margin (m)", Range(0, 1)) = 0.2

        [Header(Cut State)]
        _PartialCutHeight ("Partly Cut Height", Range(0, 1)) = 0.7
        _PartialCutColor ("Partly Cut Tint (A = amount)", Color) = (0.75, 0.70, 0.35, 0.45)
        _CutStubbleHeight ("Cut Stubble Height", Range(0, 0.5)) = 0

        [Header(Feedback)]
        _LockedFlashColor ("Locked Flash (A = amount)", Color) = (0.55, 0.60, 0.95, 0.8)
        _ProtectedFlashColor ("Protected Hit Flash (A = amount)", Color) = (1.0, 0.15, 0.10, 0.9)
        _FlashPulseSpeed ("Flash Pulse Speed", Range(0, 40)) = 18
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GrassFoliageForwardVertex
            #pragma fragment GrassFoliageForwardFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Include/GrassFoliage.hlsl"
            ENDHLSL
        }

        // Disable with Material.SetShaderPassEnabled("ShadowCaster", false) for short grass on low-end devices.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GrassFoliageDepthVertex
            #pragma fragment GrassFoliageShadowFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #define GRASS_FOLIAGE_SHADOW_CASTER
            #include "Include/GrassFoliage.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GrassFoliageDepthVertex
            #pragma fragment GrassFoliageDepthFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Include/GrassFoliage.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GrassFoliageDepthVertex
            #pragma fragment GrassFoliageDepthNormalsFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Include/GrassFoliage.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
