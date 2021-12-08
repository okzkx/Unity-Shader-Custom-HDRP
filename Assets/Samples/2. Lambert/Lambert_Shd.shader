Shader "Custom/Lambert" {
    Properties {
        [MainColor]_BaseColor("BaseColor",Color)=(1,1,1,1)
        [MainTexture]_MainTex("MainTex",2D)="white"{}
    }

    SubShader {
        Tags {
            "RenderPipeline"="HDRenderPipeline"
        }

        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardOnly"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            //-------------------------------------------------------------------------------------
            // library include
            //-------------------------------------------------------------------------------------

            // HDRP Library
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            // Local library
            
            #include "../ShaderLibrary/CustomLight.hlsl"

            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            struct AttributesMesh
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv0:TEXCOORD;
            };

            struct VaryingsMeshToPS
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord0 : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            //-------------------------------------------------------------------------------------
            // properties declaration
            //-------------------------------------------------------------------------------------

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

            float4 _MainTex_ST;
            float4 _BaseColor;

            CBUFFER_END

            //-------------------------------------------------------------------------------------
            // functions
            //-------------------------------------------------------------------------------------

            VaryingsMeshToPS Vert(AttributesMesh inputMesh)
            {
                VaryingsMeshToPS o;
                o.positionCS = TransformObjectToHClip(inputMesh.positionOS);
                o.texCoord0 = TRANSFORM_TEX(inputMesh.uv0, _MainTex);
                o.normalWS = TransformObjectToWorldNormal(inputMesh.normalOS, true);
                return o;
            }

            void Frag(VaryingsMeshToPS input, out float4 outColor : SV_Target0)
            {
                Light light = GetMainLight();
                // L(Luminance) : Radiance input
                float3 Li = saturate(light.color);
                // E(Illuminance) : To simulate the Irradiance in BRDF
                float3 E = Li * saturate(dot(input.normalWS, light.dirWS));
                // albedo : material surface color
                float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0).rgb * _BaseColor.rgb;
                // Calculate Radiance output only use diffuse equation
                float3 Lo = albedo * E;
                
                outColor = float4(Lo, 1);
            }
            ENDHLSL
        }
    }
}