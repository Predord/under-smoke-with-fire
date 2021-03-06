Shader "Custom/Water"
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
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"  }
        LOD 200

        CGPROGRAM
        #include "QuadCellData.cginc"
        #pragma surface surf StandardSpecular alpha vertex:vert
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

			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
			float4 cell2 = GetCellData(v, 2);
            float4 cell3 = GetCellData(v, 3);

			data.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y + cell2.x * v.color.z + cell3.x * v.color.w;
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
            data.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z + cell3.y * v.color.w;
		}

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            fixed4 c = _Color;
            float explored = IN.visibility.y;
            o.Albedo = c.rgb * IN.visibility.x;
            o.Specular = 0;
            o.Smoothness = _Glossiness;
            o.Occlusion = explored;
            o.Alpha = c.a;
        }
        ENDCG
    }
}
