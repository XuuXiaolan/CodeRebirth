Shader "FullScreen/SpongePosterizeNew"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

    float4 _SpongeCameraColorBuffer_TexelSize;
    float _OutlineThickness;
    float _DepthThreshold;
    float _DepthCurve;
    float _DepthStrength;
    float _ColorThreshold;
    float _ColorCurve;
    float _ColorStrength;

    float3 PreprocessColor(float3 initialColor) 
    {
        // Bright Mask
        float intensity = length(initialColor);
        float brightsMask = saturate((intensity - 0.01) * 100000.0);

        float3 brightContribution = initialColor * brightsMask;

        // Luminance Mask
        float luminance = dot(initialColor, float3(0.212673, 0.715152, 0.072175));

        float lumMod = -luminance + 0.656266;
        lumMod = dot(lumMod.xxx, lumMod.xxx);
        lumMod = sqrt(lumMod.x);
        lumMod = lumMod - 1;
        float luminanceMask = saturate(1.0 - lumMod * 100000.0);

        float3 lightenPathResult = 1.0 - (2.0 * (1.0 - initialColor * brightsMask)) * (1.0 - luminanceMask);
        float3 darkenPathResult = 2.0 * brightContribution * luminanceMask;

        float3 blendSource = (brightContribution < 0.5) ? darkenPathResult : lightenPathResult;

        float3 blendedColor = (initialColor * -brightsMask) + blendSource;

        float3 finalColor = blendedColor * 0.3 + brightContribution;

        return finalColor;
    }

    float SobelOperator_Color(float2 coord, float thickness)
    {
        float exposureValue = LOAD_TEXTURE2D_X(_ExposureTexture, uint2(0,0)).r;
        float compensatedExposure = exposureValue * 1; // Temp? Original seems to always use 1 here.

        if (compensatedExposure == 0) {
            compensatedExposure = 1;
        }

        float exposureMultiplier = rcp(compensatedExposure);

	    float3 topleft = CustomPassSampleCameraColor(coord + float2( -thickness, thickness), 0);
        topleft = topleft * exposureMultiplier;
        float3 topmid = CustomPassSampleCameraColor(coord + float2( 0.0, thickness), 0);
        topmid = topmid * exposureMultiplier;
        float3 topright = CustomPassSampleCameraColor(coord + float2( thickness, thickness), 0);
        topright = topright * exposureMultiplier;
        float3 midleft = CustomPassSampleCameraColor(coord + float2( -thickness, 0.0), 0);
        midleft = midleft * exposureMultiplier;
        float3 midright = CustomPassSampleCameraColor(coord + float2( thickness, 0.0), 0);
        midright = midright * exposureMultiplier;
        float3 bottomleft = CustomPassSampleCameraColor(coord + float2( -thickness, -thickness), 0);
        bottomleft = bottomleft * exposureMultiplier;
        float3 bottommid = CustomPassSampleCameraColor(coord + float2( 0.0, -thickness), 0);
        bottommid = bottommid * exposureMultiplier;
        float3 bottomright = CustomPassSampleCameraColor(coord + float2( thickness, -thickness), 0);
        bottomright = bottomright * exposureMultiplier;
    
        float3 Gx = (topright + 2.0*midright + bottomright) - (topleft + 2.0*midleft + bottomleft);
        float3 Gy = (topleft + 2.0*topmid + topright) - (bottomleft + 2.0*bottommid + bottomright);
    
        float r_mag = length(float2(Gx.r, Gy.r));
        float g_mag = length(float2(Gx.g, Gy.g));
        float b_mag = length(float2(Gx.b, Gy.b));
    
        // Return strongest edge
        return max(r_mag, max(g_mag, b_mag));
    }

    float SobelOperator_Depth(float2 coord, float thickness)
    {
        float2 pixelOffset = thickness * _ScreenParams.xy;

	    float topleft = LoadCameraDepth(coord + float2( -pixelOffset.x, pixelOffset.y));
        float topmid = LoadCameraDepth(coord + float2( 0.0, pixelOffset.y));
        float topright = LoadCameraDepth(coord + float2( pixelOffset.x, pixelOffset.y));
        float midleft = LoadCameraDepth(coord + float2( -pixelOffset.x, 0.0));
        float midright = LoadCameraDepth(coord + float2( pixelOffset.x, 0.0));
        float bottomleft = LoadCameraDepth(coord + float2( -pixelOffset.x, -pixelOffset.y));
        float bottommid = LoadCameraDepth(coord + float2( 0.0, -pixelOffset.y));
        float bottomright = LoadCameraDepth(coord + float2( pixelOffset.x, -pixelOffset.y));

        float Gx = (topright + 2.0*midright + bottomright) - (topleft + 2.0*midleft + bottomleft);
        float Gy = (topleft + 2.0*topmid + topright) - (bottomleft + 2.0*bottommid + bottomright);

        // Return gradient magnitude
        return length(float2(Gx, Gy));
    }

    float ProcessSingleEdge(float rawEdge, float threshold, float curve, float strength) 
    {
        // Threshold
        float invThreshold = 1.0 / threshold;
        float normalizedEdge = saturate(rawEdge * invThreshold);

        // Smoothstep
        float smoothEdge = smoothstep(0.0, 1.0, normalizedEdge);

        // Curve Adjustment
        float curvedEdge = pow(smoothEdge, curve);

        // Scaling
        return curvedEdge * strength;
    }

    float4 FullScreenReadPosterize(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float2 cs = varyings.positionCS.xy;
        float rawDepth = LoadCameraDepth(cs);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, rawDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float2 uv = posInput.positionNDC.xy;
        float3 baseColor = CustomPassSampleCameraColor(uv, 0);

        // Early escape to avoid drawing over the skybox.
        if (rawDepth == 0.0) {
            return float4(baseColor, 1.0);
        }

        float3 processedColor = PreprocessColor(baseColor);

        // Edge Detection
        float depthEdge = SobelOperator_Depth(cs, _OutlineThickness);
        depthEdge = ProcessSingleEdge(depthEdge, _DepthThreshold, _DepthCurve, _DepthStrength);

        float colorEdge = SobelOperator_Color(uv, _OutlineThickness);
        colorEdge = ProcessSingleEdge(colorEdge, _ColorThreshold, _ColorCurve, _ColorStrength);

        // Process edges
        float outlineStrength = max(depthEdge, colorEdge);

        // Apply outline
        float3 outlinedColor = processedColor * (1.0 - outlineStrength);

        // Volumetrics
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

        float3 atmosphericFogColor, transmittance;
        EvaluateAtmosphericScattering(posInput, viewDirection, atmosphericFogColor, transmittance);

        float3 finalColor = outlinedColor;

        transmittance = 1 - transmittance;
        finalColor *= transmittance;
        finalColor += atmosphericFogColor;

        return float4(finalColor, 1.0);
    }

    TEXTURE2D_X(_PosterizationBuffer);

    // Extra write pass, since we can't read/write into the camera color buffer on the same pass.
    float4 FullScreenWritePosterize(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float2 cs = varyings.positionCS.xy;
        float4 posterize = LOAD_TEXTURE2D_X_LOD(_PosterizationBuffer, cs, 0);

        return float4(posterize.rgba);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "ReadColor"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenReadPosterize
            ENDHLSL
        }
        Pass
        {
            Name "WriteColor"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenWritePosterize
            ENDHLSL
        }
    }
    Fallback Off
}
