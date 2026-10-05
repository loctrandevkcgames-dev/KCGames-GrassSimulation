// Tire tracks the mower wheels press into the lawn. Drawn on the ribbon mesh built by TireTrackRenderer.
//
// UV.x runs across the track (0..1), UV.y is the distance travelled in metres, so the tread keeps its size in
// metres at any speed. Vertex colour alpha is the age fade written by TireTrackRenderer. The track multiplies the
// ground below it, so it reads as pressed lawn on grass and as pressed soil on dirt.
Shader "GrassSimulation/TireTrack"
{
    Properties
    {
        [Header(Shape)]
        _Color ("Darken Color (A = strength)", Color) = (0.45, 0.42, 0.36, 0.75)
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.3

        [Header(Tread)]
        _TreadPeriod ("Tread Period (m)", Range(0.02, 1)) = 0.09
        _TreadSlant ("Tread Chevron Slant", Range(0, 2)) = 0.6
        _TreadDepth ("Tread Depth", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-50"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "TireTrack"
            Tags { "LightMode" = "UniversalForward" }

            Blend DstColor Zero
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TrackVertex
            #pragma fragment TrackFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _EdgeSoftness;
                float _TreadPeriod;
                float _TreadSlant;
                half _TreadDepth;
            CBUFFER_END

            struct TrackAttributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
            };

            struct TrackVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half fade         : TEXCOORD1;
            };

            TrackVaryings TrackVertex(TrackAttributes input)
            {
                TrackVaryings output = (TrackVaryings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fade = input.color.a;
                return output;
            }

            half4 TrackFragment(TrackVaryings input) : SV_Target
            {
                float across = input.uv.x;
                half edge = (half)(smoothstep(0.0, _EdgeSoftness, across) * smoothstep(0.0, _EdgeSoftness, 1.0 - across));

                // Chevron tread: blocks along the travel direction, bent towards the track centre.
                float treadCoord = input.uv.y / _TreadPeriod + abs(across - 0.5) * _TreadSlant;
                float treadAA = max(fwidth(treadCoord), 1e-4);
                half tread = (half)smoothstep(0.5 - treadAA, 0.5 + treadAA, frac(treadCoord));
                half treadMask = lerp(1.0h - _TreadDepth, 1.0h, tread);

                half alpha = saturate(_Color.a * input.fade * edge * treadMask);
                return half4(lerp(half3(1.0h, 1.0h, 1.0h), _Color.rgb, alpha), 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
