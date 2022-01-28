Shader "Custom/DetailedFeatures"
{
     Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        [Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0

        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _Specular ("Specular", Color) = (0.2, 0.2, 0.2)
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMap ("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0

        // Blending state
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }

    CGINCLUDE
        #define UNITY_SETUP_BRDF_INPUT MetallicSetup
    ENDCG

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "PerformanceChecks"="False" }
        LOD 300

        //Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        CGPROGRAM
        //#include "UnityStandardCoreForward.cginc"
        #include "QuadCellData.cginc"
        #pragma target 3.0

        // TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
        #pragma exclude_renderers gles
 
 
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
        //#pragma shader_feature_local _ALPHATEST_ON
        #pragma shader_feature_local _EMISSION
        //#pragma shader_feature _METALLICGLOSSMAP
        #pragma shader_feature_local ___ _DETAIL_MULX2
        //#pragma shader_feature _PARALLAXMAP
 
        // may not need these (not sure)
        //#pragma multi_compile_fwdbase
        //#pragma multi_compile_fog
        #pragma multi_compile_instancing
        #pragma multi_compile_shadowcaster
        #pragma surface surf StandardSpecular addshadow fullforwardshadows vertex:vert alphatest:_Cutoff

        #pragma multi_compile _ QUAD_MAP_EDIT_MODE

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 visibility;
        };

        half _Glossiness;
        fixed3 _Specular;
        fixed4 _Color;

        void vert (inout appdata_full v, out Input data) {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            float3 pos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
            float2 cellDataCoordinates;
            cellDataCoordinates.x = round(pos.x / 1.5f);
            cellDataCoordinates.y = round(pos.z / 1.5f);

            float4 cellData = GetCellData(cellDataCoordinates);
            data.visibility.x = cellData.x;
            data.visibility.x = lerp(0.25, 1, data.visibility.x);
            data.visibility.y = cellData.y;
        }

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            float explored = IN.visibility.y;
            o.Albedo = c.rgb * (IN.visibility.x * explored);
            o.Specular = 0;
            o.Smoothness = _Glossiness;
            o.Emission = c.rgb * (0.05 + explored * 0.15);
            o.Occlusion = explored;
            o.Alpha = c.a;
            clip(c.a - 0.5);
        }
        ENDCG
        // It seems Blend command is getting overridden later
        // in the processing of  Surface shader.
        //Blend [_SrcBlend] [_DstBlend]
        
        //UsePass "Standard/ShadowCaster"
    }

    FallBack "VertexLit"
    CustomEditor "StandardShaderGUI"
}
