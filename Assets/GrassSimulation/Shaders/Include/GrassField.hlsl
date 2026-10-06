#ifndef GRASS_SIMULATION_GRASS_FIELD_INCLUDED
#define GRASS_SIMULATION_GRASS_FIELD_INCLUDED

// Grass Route field contract shared by every GrassSimulation shader.
//
// The gameplay map is a grid of 0.25 x 0.25 m logic cells (GDD section 5). The CPU simulation owns the cells and
// mirrors their visual state into one small texture, one texel per cell, that all field shaders read.
//
// Global textures and vectors, set with Shader.SetGlobalTexture / Shader.SetGlobalVector:
//
//   _GrassCellState     Texture2D, RGBA32, linear (not sRGB), no mipmaps, cellsX x cellsZ texels.
//                       Texel (0, 0) is the cell at the field's minimum world XZ corner.
//                       R  Clearance 0..1. 0 = plants standing, (0, 1) = partly cut, 1 = clear ground
//                          (cut, or a cell that never had plants).
//                       G  Contact shake 0..1. Set to 1 while the blade touches the cell, then decayed by the CPU.
//                       B  Locked flash 0..1. Pulse when the blade touches a plant whose RequiredTier is too high.
//                       A  Protected hit flash 0..1. Pulse when the blade touches a protected flower cell.
//
//   _GrassLitter        Texture2D, RGBA32, linear, no mipmaps, same size and layout as _GrassCellState.
//                       RGB  Leaf clippings colour of the cell's plant kind, premultiplied by A.
//                       A    Clippings left on the ground 0..1. 0 = none (standing plants, or a cell that never had
//                            plants), 1 = the cell's plants are fully cut. Never decays during a level.
//
//   _GrassLitterAccent  Texture2D, RGBA32, linear, no mipmaps, same layout as _GrassLitter.
//                       RGB  Accent chip colour (flower petals, for example), premultiplied by _GrassLitter.A.
//                       A    Accent share of the clippings, premultiplied by _GrassLitter.A. 0 = leaves only.
//
//   _GrassCutState      Texture2D, RGBA32, linear, no mipmaps, same layout as _GrassLitter.
//                       RG   Mower heading (world XZ) when the cell was cut, mapped -1..1 -> 0..1 and premultiplied
//                            by _GrassLitter.A. 0.5 (after un-premultiplying) = no heading.
//                       B    Fresh cut 0..1, premultiplied by _GrassLitter.A. 1 right after the cut, fades over seconds.
//                       A    Cut pop 0..1, NOT premultiplied, read with point sampling. 1 at the cut, 0 about 0.1 s
//                            later; foliage sinks and splays out while it runs.
//
//   _GrassSweep         xy = world XZ origin of the celebration sweep, z = start time (_Time.y),
//                       w = 1 while a sweep is set, 0 for none.
//
//   _GrassFieldParams   xy = world XZ of the field's minimum corner, zw = 1 / field size in metres (X, Z).
//                       zw = 0 means no field is bound; every cell then reads as standing and untouched.
//
//   _GrassBladeParams   xyz = world position of the cutting blade centre, w = current cut radius in metres.
//                       Animate w over the 0.25 s upgrade transition; w = 0 hides the blade effects.
//
//   _GrassBladeState    x = protected-zone warning 0..1 for the cut range indicator. yzw are reserved.

TEXTURE2D(_GrassCellState);
TEXTURE2D(_GrassLitter);
TEXTURE2D(_GrassLitterAccent);
TEXTURE2D(_GrassCutState);

float4 _GrassFieldParams;
float4 _GrassBladeParams;
float4 _GrassBladeState;
float4 _GrassSweep;

float2 GrassFieldUV(float2 positionXZ)
{
    return (positionXZ - _GrassFieldParams.xy) * _GrassFieldParams.zw;
}

// 1 inside a bound field, 0 outside or when no field is bound.
half GrassFieldMask(float2 uv)
{
    float2 inside = step(0.0, uv) * step(uv, 1.0);
    return (half)(inside.x * inside.y * step(1e-6, _GrassFieldParams.z));
}

// Exact cell state. Use it for anything that must match the gameplay cell, such as foliage.
half4 SampleGrassCellState(float2 positionXZ)
{
    float2 uv = GrassFieldUV(positionXZ);
    return SAMPLE_TEXTURE2D_LOD(_GrassCellState, sampler_PointClamp, uv, 0) * GrassFieldMask(uv);
}

// Bilinear cell state. Use it for soft visuals, such as the cut trail on the ground.
half4 SampleGrassCellStateSmooth(float2 positionXZ)
{
    float2 uv = GrassFieldUV(positionXZ);
    return SAMPLE_TEXTURE2D_LOD(_GrassCellState, sampler_LinearClamp, uv, 0) * GrassFieldMask(uv);
}

struct GrassLitter
{
    half amount;    // clippings 0..1
    half3 leaf;     // leaf clippings colour
    half4 accent;   // rgb = accent chip colour, a = accent share 0..1
    half2 heading;  // mower heading when cut, world XZ, length 0..1
    half fresh;     // 1 right after the cut, fades to 0
};

// Bilinear clippings, un-premultiplied. Only cells that had plants and were cut leave clippings.
GrassLitter SampleGrassLitterSmooth(float2 positionXZ)
{
    float2 uv = GrassFieldUV(positionXZ);
    half mask = GrassFieldMask(uv);
    half4 litter = SAMPLE_TEXTURE2D_LOD(_GrassLitter, sampler_LinearClamp, uv, 0) * mask;
    half4 litterAccent = SAMPLE_TEXTURE2D_LOD(_GrassLitterAccent, sampler_LinearClamp, uv, 0) * mask;
    half4 cutState = SAMPLE_TEXTURE2D_LOD(_GrassCutState, sampler_LinearClamp, uv, 0) * mask;
    half inverse = 1.0h / max(litter.a, 1.0h / 255.0h);

    GrassLitter output;
    output.amount = litter.a;
    output.leaf = saturate(litter.rgb * inverse);
    output.accent = saturate(litterAccent * inverse);
    output.heading = saturate(cutState.rg * inverse) * 2.0h - 1.0h;
    output.fresh = saturate(cutState.b * inverse);
    return output;
}

// Exact cut pop of the cell, 0..1. Use it for foliage.
half SampleGrassCutPop(float2 positionXZ)
{
    float2 uv = GrassFieldUV(positionXZ);
    return SAMPLE_TEXTURE2D_LOD(_GrassCutState, sampler_PointClamp, uv, 0).a * GrassFieldMask(uv);
}

half GrassBladeRadius()
{
    return (half)_GrassBladeParams.w;
}

float GrassBladeDistanceXZ(float3 positionWS)
{
    return distance(positionWS.xz, _GrassBladeParams.xz);
}

// Cheap stable hash for per-position variation, 0..1.
float GrassHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Smooth value noise, 0..1.
float GrassValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = GrassHash21(i);
    float b = GrassHash21(i + float2(1.0, 0.0));
    float c = GrassHash21(i + float2(0.0, 1.0));
    float d = GrassHash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

#endif // GRASS_SIMULATION_GRASS_FIELD_INCLUDED
