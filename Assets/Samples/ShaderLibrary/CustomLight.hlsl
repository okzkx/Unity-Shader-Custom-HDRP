#ifndef CUSTOM_LIGHT_HLSL
#define CUSTOM_LIGHT_HLSL

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"

struct Light
{
    float3 color;
    float3 dirWS;
};

Light GetSimpleLight()
{
    DirectionalLightData directionalLightData = _DirectionalLightDatas[0];
    Light light;
    light.color = saturate(directionalLightData.color);
    light.dirWS = -directionalLightData.forward;
    return light;
}

#endif