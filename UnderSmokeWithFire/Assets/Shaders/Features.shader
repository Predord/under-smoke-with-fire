Shader "Custom/Features"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specular ("Specular", Color) = (0.2, 0.2, 0.2)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #include "QuadCellData.cginc"
        #pragma surface surf StandardSpecular fullforwardshadows vertex:vert
        #pragma target 3.0

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
        }
        ENDCG
    }
    FallBack "Diffuse"
}
