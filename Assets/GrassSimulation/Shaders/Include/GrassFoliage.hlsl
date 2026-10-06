#ifndef GRASS_SIMULATION_GRASS_FOLIAGE_INCLUDED
#define GRASS_SIMULATION_GRASS_FOLIAGE_INCLUDED

// Vertex deformation and passes for GrassSimulation/Foliage.
//
// Foliage is drawn from chunk meshes (GDD section 12). Each plant clump in a chunk is a group of vertices that
// share one root, and the root sits at the centre of its logic cell. Mesh data contract:
//
//   POSITION    Vertex position, object space.
//   NORMAL      Blade normal, object space.
//   COLOR       rgb = tint multiplied into the material colours (flower heads, per-clump variation).
//               a   = bend weight, 0 at the root to 1 at the tip. Wind, shake and push scale with it.
//   TEXCOORD0   xy = blade UV for _BaseMap (x across the blade, y root to tip).
//               z  = colour mode: 0 = COLOR.rgb tints the material gradient (leaves),
//                    1 = COLOR.rgb is the final albedo (flower petals and centres).
//   TEXCOORD1   xyz = clump root, object space. w = per-clump random 0..1.
//
// All vertices of a clump read the same cell, so a clump appears, droops and disappears as one unit, exactly when
// its gameplay cell changes.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "GrassField.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _TipColor;
    half _Cutoff;
    half _RootOcclusion;
    half4 _PatchColor;
    float _PatchScale;
    half4 _ShadeColor;
    half _ShadeThreshold;
    half _ShadeSoftness;
    half _ShadowStrength;
    half _AmbientStrength;
    half4 _TipHighlight;
    half _RimStrength;
    half _NormalUpBlend;
    half _Translucency;
    float4 _WindDirection;
    float _WindStrength;
    float _WindSpeed;
    float _WindFrequency;
    float _WindFlutter;
    float _ShakeAmplitude;
    float _ShakeFrequency;
    float _PushStrength;
    float _PushMargin;
    half _PartialCutHeight;
    half4 _PartialCutColor;
    half _CutStubbleHeight;
    half _CutPopSplay;
    half4 _LockedFlashColor;
    half4 _ProtectedFlashColor;
    half _FlashPulseSpeed;
    half4 _WindHighlight;
CBUFFER_END

#if defined(GRASS_FOLIAGE_SHADOW_CASTER)
    // Set by URP ShadowUtils while rendering the shadow map.
    float3 _LightDirection;
    float3 _LightPosition;
#endif

struct GrassFoliageAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    half4 color       : COLOR;
    float4 uv         : TEXCOORD0;
    float4 clump      : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GrassFoliageVertex
{
    float3 positionWS;
    half3 normalWS;
    half4 cellState;
    half gust;
};

GrassFoliageVertex DeformGrassFoliage(GrassFoliageAttributes input)
{
    float3 rootWS = TransformObjectToWorld(input.clump.xyz);
    float3 offsetWS = TransformObjectToWorld(input.positionOS.xyz) - rootWS;
    half bend = input.color.a;
    float random = input.clump.w;
    half4 cellState = SampleGrassCellState(rootWS.xz);

    // Cut progress: a partly cut clump droops, a cut clump collapses into stubble (or vanishes at height 0).
    // Right after the cut the clump sinks and splays out over the cut pop instead of vanishing at once.
    half clearance = cellState.r;
    half isCut = step(0.99h, clearance);
    half pop = SampleGrassCutPop(rootWS.xz);
    half isPopping = step(1e-3h, pop);
    half partialHeight = lerp(1.0h, _PartialCutHeight, saturate(clearance));
    half cutHeight = lerp(_CutStubbleHeight, 1.0h, pop * pop);
    half cutSpread = lerp(step(1e-3h, _CutStubbleHeight), 1.0h + _CutPopSplay * (1.0h - pop), isPopping);
    offsetWS.y *= lerp(partialHeight, cutHeight, isCut);
    offsetWS.xz *= lerp(1.0h, cutSpread, isCut);

    float time = _Time.y;

    // Wind: a slow lean along the wind direction plus a cross flutter, phased by world position.
    float2 windDir = _WindDirection.xz;
    windDir *= rsqrt(max(dot(windDir, windDir), 1e-6));
    float phase = dot(rootWS.xz, windDir) * _WindFrequency - time * _WindSpeed + random * 6.2832;
    float gust = sin(phase) * 0.6 + sin(phase * 2.37 + random * 3.1) * 0.4;
    float flutter = sin(time * _WindSpeed * 3.1 + random * 6.2832);
    float2 windOffset = windDir * ((gust * 0.5 + 0.5) * _WindStrength)
        + float2(-windDir.y, windDir.x) * (flutter * _WindFlutter);

    // Contact shake: short, fast rattle while the blade touches the cell.
    float shakePhase = time * _ShakeFrequency + random * 6.2832;
    float2 shakeOffset = float2(sin(shakePhase), cos(shakePhase * 1.13)) * (_ShakeAmplitude * cellState.g);

    // Blade push: plants the blade passes over without cutting (locked tiers, protected flowers) part around it.
    float2 fromBlade = rootWS.xz - _GrassBladeParams.xz;
    float bladeDistance = length(fromBlade);
    float pushRadius = _GrassBladeParams.w + _PushMargin;
    float push = saturate(1.0 - bladeDistance / max(pushRadius, 1e-3)) * step(1e-4, _GrassBladeParams.w);
    float2 pushOffset = fromBlade / max(bladeDistance, 1e-3) * (push * _PushStrength);

    float2 bendOffset = (windOffset + shakeOffset + pushOffset) * (bend * bend);

    // Keep the bent blade roughly its own length by lowering it as it leans.
    offsetWS.y *= 1.0 - saturate(length(bendOffset)) * 0.5;
    offsetWS.xz += bendOffset;

    GrassFoliageVertex output;
    output.positionWS = rootWS + offsetWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.cellState = cellState;
    output.gust = (half)saturate(gust * 0.5 + 0.5);
    return output;
}

// ---------------------------------------------------------------------------------------------------------------------
// Forward pass

struct GrassFoliageVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS    : TEXCOORD2;
    half4 color       : TEXCOORD3; // rgb = vertex tint, a = bend weight
    half4 feedback    : TEXCOORD4; // x = partial cut, y = locked flash, z = protected flash, w = fog factor
    half3 extra       : TEXCOORD5; // x = world-space colour patch 0..1, y = absolute colour mode, z = gust 0..1
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

GrassFoliageVaryings GrassFoliageForwardVertex(GrassFoliageAttributes input)
{
    GrassFoliageVaryings output = (GrassFoliageVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    GrassFoliageVertex vertex = DeformGrassFoliage(input);
    half clearance = vertex.cellState.r;

    output.positionCS = TransformWorldToHClip(vertex.positionWS);
    output.uv = TRANSFORM_TEX(input.uv.xy, _BaseMap);
    output.positionWS = vertex.positionWS;
    output.normalWS = vertex.normalWS;
    output.color = input.color;
    output.feedback.x = step(1e-3h, clearance) * (1.0h - step(0.99h, clearance));
    output.feedback.y = vertex.cellState.b;
    output.feedback.z = vertex.cellState.a;
    output.feedback.w = ComputeFogFactor(output.positionCS.z);
    output.extra.x = (half)GrassValueNoise(TransformObjectToWorld(input.clump.xyz).xz * _PatchScale);
    output.extra.y = (half)input.uv.z;
    output.extra.z = vertex.gust;
    return output;
}

half4 GrassFoliageForwardFragment(GrassFoliageVaryings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC)
    : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    #if defined(_ALPHATEST_ON)
        clip(texel.a - _Cutoff);
    #endif

    half bend = input.color.a;
    half3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, bend) * input.color.rgb * texel.rgb;
    // Patches both darken and brighten: _PatchColor is the bright end, its mirror around 1 the dark end.
    half3 patchTint = lerp(2.0h - _PatchColor.rgb, _PatchColor.rgb, input.extra.x);
    albedo *= lerp(half3(1.0h, 1.0h, 1.0h), patchTint, _PatchColor.a);
    albedo *= lerp(1.0h - _RootOcclusion, 1.0h, bend);
    albedo = lerp(albedo, input.color.rgb * texel.rgb, input.extra.y);
    albedo = lerp(albedo, _PartialCutColor.rgb, input.feedback.x * _PartialCutColor.a);

    // Cartoon look: the normal leans up so a whole tuft shades as one shape, then lighting snaps into a lit and a
    // tinted shade band instead of a smooth falloff. Shadows use the same shade tint, never black.
    half3 normalWS = input.normalWS * IS_FRONT_VFACE(isFrontFace, 1.0h, -1.0h);
    normalWS = normalize(lerp(normalWS, half3(0.0h, 1.0h, 0.0h), _NormalUpBlend));

    Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
    half halfLambert = dot(normalWS, mainLight.direction) * 0.5h + 0.5h;
    half lit = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, halfLambert);
    half shadow = smoothstep(0.3h, 0.7h, mainLight.shadowAttenuation);
    lit *= lerp(1.0h, shadow, _ShadowStrength) * mainLight.distanceAttenuation;

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half backLight = saturate(dot(viewDirWS, -mainLight.direction));
    half translucency = backLight * backLight * backLight * backLight * _Translucency * bend;
    half rim = pow(1.0h - saturate(dot(viewDirWS, normalWS)), 3.0h) * _RimStrength * bend;
    half leaf = 1.0h - input.extra.y;
    rim *= leaf;
    half tip = smoothstep(0.7h, 1.0h, bend) * _TipHighlight.a * leaf;

    half3 lightColor = lerp(_ShadeColor.rgb, mainLight.color, lit);
    half3 ambient = SampleSH(half3(0.0h, 1.0h, 0.0h)) * _AmbientStrength;
    half3 color = albedo * (lightColor + ambient);
    color += (_TipHighlight.rgb * tip + albedo * (translucency + rim)) * mainLight.color * lit;

    // Wind waves: bright bands roll across the field where gusts peak, a classic cartoon grass cue.
    half wave = input.extra.z * input.extra.z * input.extra.z * bend * leaf;
    color += _WindHighlight.rgb * (_WindHighlight.a * wave);

    half pulse = 0.65h + 0.35h * sin(_Time.y * _FlashPulseSpeed);
    color = lerp(color, _LockedFlashColor.rgb, saturate(input.feedback.y * _LockedFlashColor.a * pulse));
    color = lerp(color, _ProtectedFlashColor.rgb, saturate(input.feedback.z * _ProtectedFlashColor.a * pulse));

    color = MixFog(color, input.feedback.w);
    return half4(color, 1.0h);
}

// ---------------------------------------------------------------------------------------------------------------------
// Shadow caster, depth only and depth normals passes

struct GrassFoliageDepthVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    half3 normalWS    : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

GrassFoliageDepthVaryings GrassFoliageDepthVertex(GrassFoliageAttributes input)
{
    GrassFoliageDepthVaryings output = (GrassFoliageDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    GrassFoliageVertex vertex = DeformGrassFoliage(input);

    #if defined(GRASS_FOLIAGE_SHADOW_CASTER)
        #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
            float3 lightDirectionWS = normalize(_LightPosition - vertex.positionWS);
        #else
            float3 lightDirectionWS = _LightDirection;
        #endif
        output.positionCS = ApplyShadowClamping(
            TransformWorldToHClip(ApplyShadowBias(vertex.positionWS, vertex.normalWS, lightDirectionWS)));
    #else
        output.positionCS = TransformWorldToHClip(vertex.positionWS);
    #endif

    output.uv = TRANSFORM_TEX(input.uv.xy, _BaseMap);
    output.normalWS = normalize(lerp(vertex.normalWS, half3(0.0h, 1.0h, 0.0h), _NormalUpBlend));
    return output;
}

void GrassFoliageClip(float2 uv)
{
    #if defined(_ALPHATEST_ON)
        clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a - _Cutoff);
    #endif
}

half4 GrassFoliageShadowFragment(GrassFoliageDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    GrassFoliageClip(input.uv);
    return 0;
}

half GrassFoliageDepthFragment(GrassFoliageDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    GrassFoliageClip(input.uv);
    return input.positionCS.z;
}

half4 GrassFoliageDepthNormalsFragment(GrassFoliageDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    GrassFoliageClip(input.uv);
    return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
}

#endif // GRASS_SIMULATION_GRASS_FOLIAGE_INCLUDED
