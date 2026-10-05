// Cut range indicator around the mower (GDD sections 4, 7 and 11). The ring is drawn at the exact gameplay cut
// radius from _GrassBladeParams, so it stays correct while code animates the radius during an upgrade.
// _GrassBladeState.x turns the dashed ring into a solid, pulsing warning ring near protected flowers.
//
// Put it on a flat mesh centred on the mower and at least (max cut radius + ring width) * 2 wide, for example a
// Quad rotated to face up and scaled to 3 m. Only pixels near the ring and inside it are drawn.
Shader "GrassSimulation/CutRange"
{
    Properties
    {
        [Header(Ring)]
        _RingColor ("Ring Color", Color) = (1, 1, 1, 0.9)
        _OutlineColor ("Ring Outline Color", Color) = (0, 0, 0, 0.35)
        _RingWidth ("Ring Width (m)", Range(0.01, 0.2)) = 0.05
        _OutlineWidth ("Outline Width (m)", Range(0, 0.1)) = 0.02
        _FillColor ("Inner Fill Color", Color) = (1, 1, 1, 0.08)
        _BodyClearRadius ("Body Clear Radius (m)", Range(0, 1)) = 0.35

        [Header(Dashes)]
        _DashCount ("Dash Count", Range(0, 64)) = 24
        _DashFill ("Dash Fill", Range(0.1, 1)) = 0.6
        _DashSpeed ("Dash Speed", Range(-2, 2)) = 0.15

        [Header(Warning)]
        _WarningColor ("Warning Color", Color) = (1, 0.2, 0.1, 1)
        _WarningPulseSpeed ("Warning Pulse Speed", Range(0, 30)) = 10

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+10"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "CutRange"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CutRangeVertex
            #pragma fragment CutRangeFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Include/GrassField.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RingColor;
                half4 _OutlineColor;
                float _RingWidth;
                float _OutlineWidth;
                half4 _FillColor;
                float _BodyClearRadius;
                float _DashCount;
                half _DashFill;
                float _DashSpeed;
                half4 _WarningColor;
                half _WarningPulseSpeed;
            CBUFFER_END

            struct CutRangeAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct CutRangeVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CutRangeVaryings CutRangeVertex(CutRangeAttributes input)
            {
                CutRangeVaryings output = (CutRangeVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 CutRangeFragment(CutRangeVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float radius = GrassBladeRadius();
                float2 fromCentre = input.positionWS.xz - _GrassBladeParams.xz;
                float distanceToCentre = length(fromCentre);
                float halfRing = _RingWidth * 0.5;
                float outerEdge = halfRing + _OutlineWidth;
                clip(radius + outerEdge - distanceToCentre);
                clip(radius - 1e-4);

                float aa = max(fwidth(distanceToCentre), 1e-4);
                float ringDistance = abs(distanceToCentre - radius);
                half ring = 1.0h - (half)smoothstep(halfRing - aa, halfRing + aa, ringDistance);
                half outline = (1.0h - (half)smoothstep(outerEdge - aa, outerEdge + aa, ringDistance)) - ring;
                half inside = 1.0h - (half)smoothstep(radius - halfRing - aa, radius - halfRing, distanceToCentre);
                // Keep the fill off the mower body, which sits inside the ring (GDD body radius 0.30 m).
                inside *= (half)smoothstep(_BodyClearRadius - aa, _BodyClearRadius, distanceToCentre);

                half warning = saturate((half)_GrassBladeState.x);

                // Dashes rotate slowly while safe and merge into a solid ring as the warning rises.
                float turns = atan2(fromCentre.y, fromCentre.x) * (0.5 * INV_PI) + 0.5;
                float dashCoord = frac(turns * round(_DashCount) + _Time.y * _DashSpeed);
                float dashAA = max(fwidth(turns * round(_DashCount)), 1e-4);
                half dash = 1.0h - (half)smoothstep(_DashFill - dashAA, _DashFill + dashAA, dashCoord);
                dash = _DashCount < 0.5 ? 1.0h : dash;
                ring *= lerp(dash, 1.0h, warning);
                outline *= lerp(dash, 1.0h, warning);

                half pulse = 0.75h + 0.25h * sin(_Time.y * _WarningPulseSpeed);
                half4 ringColor = lerp(_RingColor, _WarningColor * half4(1.0h, 1.0h, 1.0h, pulse), warning);
                half4 fillColor = lerp(_FillColor, _WarningColor * half4(1.0h, 1.0h, 1.0h, _FillColor.a * 2.0h), warning);

                // Premultiplied "over" compositing: fill, then outline, then ring.
                half fillAlpha = fillColor.a * inside;
                half4 color = half4(fillColor.rgb * fillAlpha, fillAlpha);
                half outlineAlpha = saturate(outline) * _OutlineColor.a;
                color = half4(_OutlineColor.rgb * outlineAlpha, outlineAlpha) + color * (1.0h - outlineAlpha);
                half ringAlpha = saturate(ring) * ringColor.a;
                color = half4(ringColor.rgb * ringAlpha, ringAlpha) + color * (1.0h - ringAlpha);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
