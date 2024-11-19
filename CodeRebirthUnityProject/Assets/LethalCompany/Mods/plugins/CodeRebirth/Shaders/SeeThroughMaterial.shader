Shader "Custom/SeeThroughShader"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _MainColor ("Fill Color", Color) = (1,1,1,1)
        _WireframeVal ("Wireframe width", Range(0., 1.)) = 0.05
        _MaxVisibilityDistance ("Max Visibility Distance", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            ZWrite Off
            ZTest Greater
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _EdgeColor;
                float4 _MainColor;
                float _WireframeVal;
                float _MaxVisibilityDistance;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.positionCS.xy / _ScreenSize.xy;
                float sceneDepth = LoadCameraDepth(input.positionCS.xy);
                float linearDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);

                // Calculate distance-based fade
                float fadeAlpha = saturate((_MaxVisibilityDistance - linearDepth) / _MaxVisibilityDistance);

                // Apply wireframe effect
                float3 screenDeriv = fwidth(input.positionWS);
                float edgeFactor = min(1.0 - saturate((length(screenDeriv.xy) / _WireframeVal)), 1);

                float4 color = lerp(_MainColor, _EdgeColor, edgeFactor);
                color.a *= fadeAlpha;

                return color;
            }
            ENDHLSL
        }
    }
}