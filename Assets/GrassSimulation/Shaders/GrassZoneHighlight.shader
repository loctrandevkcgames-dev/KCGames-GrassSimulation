// Ground overlay for marked zones: protected flower beds (border plus hatching, so colour is not the only signal,
// GDD sections 4 and 11) and objective zones shown in Preview or after a failure (GDD section 7).
//
// Put it on a 1 x 1 m Quad rotated to face up; scale the Quad's X and Y to the zone size in metres. Border, corner
// and hatch sizes stay in metres at any scale. Drive _Flash 0..1 from code when the blade hits the zone.
Shader "GrassSimulation/ZoneHighlight"
{
    Properties
    {
        [Header(Shape)]
        _Color ("Color", Color) = (1, 0.25, 0.2, 1)
        _BorderWidth ("Border Width (m)", Range(0.01, 0.5)) = 0.08
        _CornerRadius ("Corner Radius (m)", Range(0, 2)) = 0.25
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.12

        [Header(Hatching)]
        _HatchAlpha ("Hatch Alpha", Range(0, 1)) = 0.25
        _HatchWidth ("Hatch Period (m)", Range(0.05, 2)) = 0.35
        _HatchSpeed ("Hatch Scroll Speed", Range(-2, 2)) = 0.2

        [Header(Animation)]
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.25
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.5
        _Flash ("Hit Flash", Range(0, 1)) = 0
        _FlashColor ("Hit Flash Color", Color) = (1, 0.1, 0.05, 1)

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "ZoneHighlight"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ZoneVertex
            #pragma fragment ZoneFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _BorderWidth;
                float _CornerRadius;
                half _FillAlpha;
                half _HatchAlpha;
                float _HatchWidth;
                float _HatchSpeed;
                half _PulseAmount;
                half _PulseSpeed;
                half _Flash;
                half4 _FlashColor;
            CBUFFER_END

            struct ZoneAttributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ZoneVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 localPosition : TEXCOORD0; // metres from the zone centre
                nointerpolation float2 halfSize : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ZoneVaryings ZoneVertex(ZoneAttributes input)
            {
                ZoneVaryings output = (ZoneVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float2 size = float2(length(objectToWorld._m00_m10_m20), length(objectToWorld._m01_m11_m21));
                output.halfSize = size * 0.5;
                output.localPosition = (input.uv - 0.5) * size;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 ZoneFragment(ZoneVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Rounded rectangle signed distance in metres: negative inside, 0 on the edge.
                float cornerRadius = min(_CornerRadius, min(input.halfSize.x, input.halfSize.y));
                float2 q = abs(input.localPosition) - (input.halfSize - cornerRadius);
                float sdf = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - cornerRadius;
                float aa = max(fwidth(sdf), 1e-4);

                half inside = 1.0h - (half)smoothstep(-aa, 0.0, sdf);
                half border = inside * (half)smoothstep(-_BorderWidth - aa, -_BorderWidth, sdf);

                float hatchCoord = (input.positionWS.x + input.positionWS.z) / _HatchWidth + _Time.y * _HatchSpeed;
                float hatchAA = max(fwidth(hatchCoord), 1e-4);
                half hatch = (half)smoothstep(0.5 - hatchAA, 0.5 + hatchAA, frac(hatchCoord));

                half pulse = 1.0h - _PulseAmount * (0.5h + 0.5h * sin(_Time.y * _PulseSpeed));
                half3 rgb = lerp(_Color.rgb, _FlashColor.rgb, _Flash);
                half fillAlpha = (_FillAlpha + hatch * _HatchAlpha) * (1.0h + _Flash);
                half alpha = lerp(saturate(fillAlpha) * inside, 1.0h, border) * _Color.a * pulse;
                alpha = saturate(alpha + _Flash * 0.35h * inside);
                return half4(rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
