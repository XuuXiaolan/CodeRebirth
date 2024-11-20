Shader "HDRP/Decal" {
	Properties {
		_BaseColor ("_BaseColor", Vector) = (1,1,1,1)
		_BaseColorMap ("BaseColorMap", 2D) = "white" {}
		_NormalMap ("NormalMap", 2D) = "bump" {}
		_MaskMap ("MaskMap", 2D) = "white" {}
		_DecalBlend ("_DecalBlend", Range(0, 1)) = 0.5
		_NormalBlendSrc ("_NormalBlendSrc", Float) = 0
		_MaskBlendSrc ("_MaskBlendSrc", Float) = 1
		[Enum(Depth Bias, 0, View Bias, 1)] _DecalMeshBiasType ("_DecalMeshBiasType", Float) = 0
		_DecalMeshDepthBias ("_DecalMeshDepthBias", Float) = 0
		_DecalMeshViewBias ("_DecalMeshViewBias", Float) = 0
		_DrawOrder ("_DrawOrder", Float) = 0
		[HDR] _EmissiveColor ("EmissiveColor", Vector) = (0,0,0,1)
		[HideInInspector] _EmissiveColorLDR ("EmissiveColor LDR", Vector) = (0,0,0,1)
		[HideInInspector] [HDR] _EmissiveColorHDR ("EmissiveColor HDR", Vector) = (0,0,0,1)
		_EmissiveColorMap ("EmissiveColorMap", 2D) = "white" {}
		_EmissiveIntensityUnit ("Emissive Mode", Float) = 0
		[ToggleUI] _UseEmissiveIntensity ("Use Emissive Intensity", Float) = 0
		_EmissiveIntensity ("Emissive Intensity", Float) = 1
		_EmissiveExposureWeight ("Emissive Pre Exposure", Range(0, 1)) = 1
		_MetallicRemapMin ("_MetallicRemapMin", Range(0, 1)) = 0
		_MetallicRemapMax ("_MetallicRemapMax", Range(0, 1)) = 1
		_SmoothnessRemapMin ("SmoothnessRemapMin", Float) = 0
		_SmoothnessRemapMax ("SmoothnessRemapMax", Float) = 1
		_AORemapMin ("AORemapMin", Float) = 0
		_AORemapMax ("AORemapMax", Float) = 1
		_DecalMaskMapBlueScale ("_DecalMaskMapBlueScale", Range(0, 1)) = 1
		_Smoothness ("_Smoothness", Range(0, 1)) = 0.5
		_Metallic ("_Metallic", Range(0, 1)) = 0
		_AO ("_AO", Range(0, 1)) = 1
		[ToggleUI] _AffectAlbedo ("Boolean", Float) = 1
		[ToggleUI] _AffectNormal ("Boolean", Float) = 1
		[ToggleUI] _AffectAO ("Boolean", Float) = 0
		[ToggleUI] _AffectMetal ("Boolean", Float) = 1
		[ToggleUI] _AffectSmoothness ("Boolean", Float) = 1
		[ToggleUI] _AffectEmission ("Boolean", Float) = 0
		[HideInInspector] _DecalStencilRef ("_DecalStencilRef", Float) = 16
		[HideInInspector] _DecalStencilWriteMask ("_DecalStencilWriteMask", Float) = 16
		[HideInInspector] _DecalColorMask0 ("_DecalColorMask0", Float) = 0
		[HideInInspector] _DecalColorMask1 ("_DecalColorMask1", Float) = 0
		[HideInInspector] _DecalColorMask2 ("_DecalColorMask2", Float) = 0
		[HideInInspector] _DecalColorMask3 ("_DecalColorMask3", Float) = 0
		[HideInInspector] _Unity_Identify_HDRP_Decal ("_Unity_Identify_HDRP_Decal", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = 1;
		}
		ENDCG
	}
	Fallback "Hidden/HDRP/FallbackError"
	//CustomEditor "Rendering.HighDefinition.DecalUI"
}