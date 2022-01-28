Shader "Custom/Terrain"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _TargetColor ("Target Color", Color) = (1,0,0,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "white" {}
        _GridTex ("Grid Texture", 2D) = "white" {}
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
        #pragma target 3.5

        #pragma multi_compile _ GRID_ON
        #pragma multi_compile _ QUAD_MAP_EDIT_MODE

        sampler2D _GridTex;
        UNITY_DECLARE_TEX2DARRAY(_MainTex);

        struct Input
        {
			float4 color: COLOR;
            float3 worldPos;
            float3 worldNormal;
            float4 terrain;
            float4 visibility;
            float explored;
            float targeted;
        };

        half _Glossiness;
        fixed3 _Specular;
        fixed4 _Color;
        fixed4 _TargetColor;

        void vert (inout appdata_full v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);

            float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
			float4 cell2 = GetCellData(v, 2);
            float4 cell3 = GetCellData(v, 3);

			data.terrain.x = cell0.w;
			data.terrain.y = cell1.w;
			data.terrain.z = cell2.w;
            data.terrain.w = cell3.w;

            data.visibility.x = cell0.x;
			data.visibility.y = cell1.x;
			data.visibility.z = cell2.x;
            data.visibility.w = cell3.x;
            data.visibility = lerp(0.25, 1, data.visibility);
            data.explored = cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z + cell3.y * v.color.w;
            data.targeted = cell0.z * v.color.x + cell1.z * v.color.y + cell2.z * v.color.z + cell3.z * v.color.w;
		}

        float4 GetTerrainColor (Input IN, int index) {
            float3 uvs = IN.worldPos.xyz * 0.3;
            float3 blending = saturate(abs(IN.worldNormal.xyz) - 0.2);
            blending = pow(blending, 2.0);
            blending /= dot(blending, float3(1.0, 1.0, 1.0));
			float4 c = blending.x * UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uvs.yz, IN.terrain[index]));
            c = blending.y * UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uvs.xz, IN.terrain[index])) + c;
            c = blending.z * UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uvs.xy, IN.terrain[index])) + c;
			return c * (IN.color[index] * IN.visibility[index]);
		}

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            fixed4 c =
				GetTerrainColor(IN, 0) +
				GetTerrainColor(IN, 1) +
				GetTerrainColor(IN, 2) +
                GetTerrainColor(IN, 3);
            fixed4 grid = 1;
            #if defined(GRID_ON)
                float2 gridUV = IN.worldPos.xz;
                gridUV.x -= 0.75;
                gridUV.x *= 1 / (2 * 0.75);
                gridUV.y -= 0.75;
                gridUV.y *= 1 / (2 * 0.75);
                grid = tex2D(_GridTex, gridUV);
            #endif
            fixed4 targetedColor = max(_TargetColor, IN.targeted);
            o.Albedo = c.rgb * grid * _Color * IN.explored * targetedColor;
            o.Specular = 0;
            o.Smoothness = _Glossiness;
            o.Emission = c.rgb * grid * _Color * (0.1 + IN.explored * 0.12);
            o.Occlusion = IN.explored;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
