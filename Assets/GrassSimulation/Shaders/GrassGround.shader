// Lawn ground of the Grass Route field. Cells with standing plants show the uncut colour; cleared cells (cut or empty)
// show the mown colour with mower stripes, so every cut leaves a clean, readable trail (GDD sections 2 and 11).
// Reads the cell state contract documented in Include/GrassField.hlsl. _CutColor is the cut trail skin colour.
Shader "GrassSimulation/Ground"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("Ground Texture (world tiled)", 2D) = "white" {}
        _BaseMapWorldSize ("Ground Texture Size (m)", Float) = 4
        [MainColor] _CutColor ("Cut Trail Color", Color) = (0.62, 0.82, 0.38, 1)
        _UncutColor ("Uncut Ground Color", Color) = (0.16, 0.32, 0.10, 1)
        _NoiseScale ("Variation Scale (1/m)", Float) = 0.6
        _NoiseStrength ("Variation Strength", Range(0, 0.5)) = 0.12

        [Header(Soil Pattern)]
        _BlotchColor ("Blotch Tint (A = amount)", Color) = (0.78, 0.66, 0.6, 0.5)
        _BlotchScale ("Blotch Scale (1/m)", Float) = 0.9
        _BlotchCoverage ("Blotch Coverage", Range(0, 1)) = 0.45
        _PebbleColor ("Pebble Color (A = amount)", Color) = (0.8, 0.68, 0.56, 1)
        _PebbleScale ("Pebble Grid (1/m)", Float) = 3
        _PebbleDensity ("Pebble Density", Range(0, 1)) = 0.35
        _PebbleSize ("Pebble Size (cell)", Range(0.05, 0.45)) = 0.2
        _SpeckColor ("Speck Tint (A = amount)", Color) = (0.62, 0.48, 0.38, 0.6)
        _SpeckScale ("Speck Grid (1/m)", Float) = 14
        _SpeckDensity ("Speck Density", Range(0, 1)) = 0.25

        [Header(Cut Trail)]
        _CutEdge ("Cut Edge Threshold", Range(0.05, 0.95)) = 0.5
        _CutEdgeSoftness ("Cut Edge Softness", Range(0.01, 0.5)) = 0.2
        _CutEdgeDarkening ("Cut Edge Darkening", Range(0, 1)) = 0.25

        [Header(Mower Stripes)]
        _StripeStrength ("Stripe Strength", Range(0, 0.5)) = 0.12
        _StripeWidth ("Stripe Width (m)", Float) = 1
        _StripeAngle ("Stripe Angle (deg)", Range(0, 180)) = 0

        [Header(Cartoon Lighting)]
        _ShadeColor ("Shade Color", Color) = (0.32, 0.45, 0.62, 1)
        _ShadeThreshold ("Shade Threshold", Range(0, 1)) = 0.45
        _ShadeSoftness ("Shade Softness", Range(0.001, 0.5)) = 0.06
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.85
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.25
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Include/GrassField.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float _BaseMapWorldSize;
            half4 _CutColor;
            half4 _UncutColor;
            float _NoiseScale;
            half _NoiseStrength;
            half4 _BlotchColor;
            float _BlotchScale;
            half _BlotchCoverage;
            half4 _PebbleColor;
            float _PebbleScale;
            half _PebbleDensity;
            half _PebbleSize;
            half4 _SpeckColor;
            float _SpeckScale;
            half _SpeckDensity;
            half _CutEdge;
            half _CutEdgeSoftness;
            half _CutEdgeDarkening;
            half _StripeStrength;
            float _StripeWidth;
            float _StripeAngle;
            half4 _ShadeColor;
            half _ShadeThreshold;
            half _ShadeSoftness;
            half _ShadowStrength;
            half _AmbientStrength;
        CBUFFER_END

        struct GroundAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GroundVertex
            #pragma fragment GroundFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct GroundVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS    : TEXCOORD1;
                half fogFactor    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Cartoon soil: two-tone blotches, scattered pebbles lit from the top of the screen with a small drop
            // shadow, and fine specks. Everything is procedural in world space, so chunks tile seamlessly.
            void NearestPebble(float2 coord, out float2 offset, out float id)
            {
                float2 cell = floor(coord);
                float2 local = frac(coord);
                float best = 8.0;
                offset = 0.0;
                id = 0.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbour = cell + float2(x, y);
                        float2 jitter = float2(GrassHash21(neighbour), GrassHash21(neighbour + 17.3));
                        float2 delta = local - (float2(x, y) + 0.2 + 0.6 * jitter);
                        float distanceSq = dot(delta, delta);

                        if (distanceSq < best)
                        {
                            best = distanceSq;
                            offset = delta;
                            id = GrassHash21(neighbour + 41.7);
                        }
                    }
                }
            }

            half3 ApplySoilPattern(half3 albedo, float2 positionXZ)
            {
                half blotchNoise = (half)GrassValueNoise(positionXZ * _BlotchScale + 13.1);
                half blotch = smoothstep(_BlotchCoverage - 0.03h, _BlotchCoverage + 0.03h, blotchNoise);
                albedo = lerp(albedo, albedo * _BlotchColor.rgb, blotch * _BlotchColor.a);

                float2 offset;
                float id;
                NearestPebble(positionXZ * _PebbleScale, offset, id);
                half exists = step(id, _PebbleDensity);
                half size = _PebbleSize * (0.6h + 0.8h * (half)frac(id * 7.13));
                half aa = (half)max(fwidth(positionXZ.x * _PebbleScale), 1e-3);

                float2 shadowOffset = offset + float2(-0.25, 0.35) * size;
                half shadow = (1.0h - smoothstep(size - aa, size + aa, (half)length(shadowOffset))) * exists;
                albedo *= 1.0h - shadow * 0.35h;

                half pebble = (1.0h - smoothstep(size - aa, size + aa, (half)length(offset))) * exists;
                half topLight = saturate(0.5h + (half)(offset.y - offset.x * 0.5) / size * 0.8h);
                half rim = smoothstep(size * 0.55h, size, (half)length(offset));
                half3 pebbleColor = _PebbleColor.rgb * lerp(0.62h, 1.15h, topLight) * (1.0h - rim * 0.18h);
                albedo = lerp(albedo, pebbleColor, pebble * _PebbleColor.a);

                float2 speckCell = floor(positionXZ * _SpeckScale);
                float2 speckJitter = float2(GrassHash21(speckCell + 9.1), GrassHash21(speckCell + 23.9)) - 0.5;
                float2 speckLocal = frac(positionXZ * _SpeckScale) - 0.5 - speckJitter * 0.6;
                half speckOn = step(GrassHash21(speckCell + 3.7), _SpeckDensity);
                half speck = (1.0h - smoothstep(0.12h, 0.2h, (half)length(speckLocal))) * speckOn * (1.0h - pebble);
                albedo = lerp(albedo, albedo * _SpeckColor.rgb, speck * _SpeckColor.a);
                return albedo;
            }

            GroundVaryings GroundVertex(GroundAttributes input)
            {
                GroundVaryings output = (GroundVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 GroundFragment(GroundVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 positionXZ = input.positionWS.xz;

                // Bilinear clearance gives the trail rounded edges instead of hard 0.25 m squares.
                half clearance = SampleGrassCellStateSmooth(positionXZ).r;
                half cutMask = smoothstep(_CutEdge - _CutEdgeSoftness, _CutEdge + _CutEdgeSoftness, clearance);
                half edge = 1.0h - abs(cutMask * 2.0h - 1.0h);

                float angle = radians(_StripeAngle);
                float stripeCoord = dot(positionXZ, float2(cos(angle), sin(angle))) / max(_StripeWidth, 1e-3);
                half stripe = (half)(step(0.5, frac(stripeCoord)) * 2.0 - 1.0);

                half3 cutColor = _CutColor.rgb * (1.0h + stripe * _StripeStrength);
                half3 albedo = lerp(_UncutColor.rgb, cutColor, cutMask);
                albedo *= 1.0h - edge * _CutEdgeDarkening;

                float2 baseUV = positionXZ / max(_BaseMapWorldSize, 1e-3) * _BaseMap_ST.xy + _BaseMap_ST.zw;
                albedo *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV).rgb;
                albedo *= 1.0h + ((half)GrassValueNoise(positionXZ * _NoiseScale) - 0.5h) * 2.0h * _NoiseStrength;
                albedo = ApplySoilPattern(albedo, positionXZ);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half halfLambert = dot(normalWS, mainLight.direction) * 0.5h + 0.5h;
                half lit = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, halfLambert);
                half shadow = smoothstep(0.3h, 0.7h, mainLight.shadowAttenuation);
                lit *= lerp(1.0h, shadow, _ShadowStrength) * mainLight.distanceAttenuation;

                half3 lightColor = lerp(_ShadeColor.rgb, mainLight.color, lit);
                half3 color = albedo * (lightColor + SampleSH(normalWS) * _AmbientStrength);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GroundDepthVertex
            #pragma fragment GroundDepthFragment
            #pragma multi_compile_instancing

            struct GroundDepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            GroundDepthVaryings GroundDepthVertex(GroundAttributes input)
            {
                GroundDepthVaryings output = (GroundDepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half GroundDepthFragment(GroundDepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GroundDepthNormalsVertex
            #pragma fragment GroundDepthNormalsFragment
            #pragma multi_compile_instancing

            struct GroundDepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS    : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            GroundDepthNormalsVaryings GroundDepthNormalsVertex(GroundAttributes input)
            {
                GroundDepthNormalsVaryings output = (GroundDepthNormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 GroundDepthNormalsFragment(GroundDepthNormalsVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
