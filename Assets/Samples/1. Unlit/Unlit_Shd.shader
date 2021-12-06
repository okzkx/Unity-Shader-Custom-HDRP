Shader "Custom/Unlit" {
    Properties {
        [MainColor] _UnlitColor("Color",Color)=(1,1,1,1)
        [MainTexture] _UnlitColorMap("ColorMap",2D)="White"{}
    }

    SubShader {
        Tags {
            "RenderPipeline" = "HDRenderPipeline"
        }

        // Unlit shader always render in forward
        Pass {
            Name "ForwardOnly"
            Tags {
                "LightMode" = "ForwardOnly"
            }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float2 texCoord0 : TEXCOORD0;
            };

            TEXTURE2D(_UnlitColorMap);
            SAMPLER(sampler_UnlitColorMap);

            CBUFFER_START(UnityPerMaterial)

            float4 _UnlitColor;
            float4 _UnlitColorMap_ST;

            CBUFFER_END

            Varyings Vert(Attributes inputMesh)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(inputMesh.positionOS);
                output.texCoord0 = inputMesh.uv0;
                return output;
            }

            void Frag(Varyings input, out float4 outColor:SV_Target0)
            {
                float2 unlitColorMapUv = TRANSFORM_TEX(input.texCoord0.xy, _UnlitColorMap);
                float3 color = SAMPLE_TEXTURE2D(_UnlitColorMap, sampler_UnlitColorMap, unlitColorMapUv).rgb * _UnlitColor.rgb;
                outColor = float4(color, 1);
            }
            ENDHLSL
        }
    }
}