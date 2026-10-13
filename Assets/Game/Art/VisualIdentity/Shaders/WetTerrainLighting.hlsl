// Retains the installed URP terrain splat, normal, instancing, shadow and fog paths.
// Only the forward PBR inputs change; no TerrainData or painted layers are rewritten.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
float4 _ReverbWeather;
half4 ReverbWetTerrainPBR(InputData inputData, half3 albedo, half metallic, half3 specular,
    half smoothness, half occlusion, half3 emission, half alpha)
{
    float2 p=inputData.positionWS.xz;
    float patch=.5+.25*sin(p.x*.83+sin(p.y*.51))+.25*sin(p.y*1.17-p.x*.37);
    half wet=_ReverbWeather.y*smoothstep(.12,.8,patch)*saturate(inputData.normalWS.y);
    albedo*=lerp(1,.68,wet);
    smoothness=lerp(smoothness,.78,wet);
    float t=_ReverbWeather.z;
    float2 ripple=float2(sin(p.x*27+t*3.1)*cos(p.y*19-t*2.5),cos(p.x*21-t*2.1)*sin(p.y*24+t*2.8));
    inputData.normalWS=normalize(inputData.normalWS+float3(ripple.x,0,ripple.y)*wet*_ReverbWeather.x*.022);
    return UniversalFragmentPBR(inputData,albedo,metallic,specular,smoothness,occlusion,emission,alpha);
}
#define UniversalFragmentPBR ReverbWetTerrainPBR
