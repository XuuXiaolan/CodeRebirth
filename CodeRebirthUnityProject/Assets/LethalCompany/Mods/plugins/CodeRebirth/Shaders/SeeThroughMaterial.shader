Shader "Hidden/Renderers/SeeThroughStencil"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _ColorMap("ColorMap", 2D) = "white" {}
        _MaxVisibilityDistance("Max Visibility Distance", Float) = 10
        // Transparency
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [HideInInspector]_StencilWriteMask("_StencilWriteMask", Float) = 0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    //enable GPU instancing support
    #pragma multi_compile_instancing

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "FirstPass"
            Tags { "LightMode" = "FirstPass" "Queue" = "Transparent" }

            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            Cull Back
            ZWrite On
            ZTest LEqual

            Stencil
            {
                Ref 1
                Comp Always
                WriteMask [_StencilWriteMask]
                Pass Replace
            }

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderers.hlsl"

            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                ZERO_INITIALIZE(BuiltinData, builtinData);
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.color = 1;
            }

            #if SHADERPASS != SHADERPASS_FORWARD_UNLIT

            #error SHADERPASS_is_not_correctly_defined
            #endif

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

            PackedVaryingsType Vert(AttributesMesh inputMesh)
            {
                VaryingsType varyingsType;
                varyingsType.vmesh = VertMesh(inputMesh);
                return PackVaryingsType(varyingsType);
            }

            #ifdef TESSELLATION_ON

            PackedVaryingsToPS VertTesselation(VaryingsToDS input)
            {
                VaryingsToPS output;
                output.vmesh = VertMeshTesselation(input.vmesh);
                return PackVaryingsToPS(output);
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

            #endif // TESSELLATION_ON

            float _MaxVisibilityDistance;
            float _AlphaCutoff;

            float4 Frag(PackedVaryingsToPS packedInput) : SV_Target
            {
                FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

                PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

                float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

                SurfaceData surfaceData;
                BuiltinData builtinData;
                GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

                BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

                float4 outColor = ApplyBlendMode(bsdfData.color + builtinData.emissiveColor, builtinData.opacity);
        
                // Calculate distance from camera
                float distanceFromCamera = length(_WorldSpaceCameraPos - input.positionRWS);
        
                // Apply distance-based fade
                float fadeAlpha = saturate((_MaxVisibilityDistance - distanceFromCamera) / _MaxVisibilityDistance);
                outColor.a *= fadeAlpha;

                // Apply alpha cutoff
                if (outColor.a <= _AlphaCutoff)
                {
                    discard;
                }

                outColor = EvaluateAtmosphericScattering(posInput, V, outColor);

                return outColor;
            }

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
}