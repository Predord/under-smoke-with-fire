sampler2D _QuadCellData;
float4 _QuadCellData_TexelSize;

float4 FilterCellData (float4 data) {
	#if defined(QUAD_MAP_EDIT_MODE)
		data.xy = 1;
	#endif
	return data;
}

float4 GetCellData (appdata_full v, int index) {
    float2 uv;
    uv.x = (v.texcoord[index] + 0.5) * _QuadCellData_TexelSize.x;
    float row = floor(uv.x);
	uv.x -= row;
    uv.y = (row + 0.5) * _QuadCellData_TexelSize.y;
    float4 data = tex2Dlod(_QuadCellData, float4(uv, 0, 0));
    data.w *= 255;
	return FilterCellData(data);
}

float4 GetCellData (float2 cellDataCoordinates) {
	float2 uv = cellDataCoordinates + 0.5;
	uv.x *= _QuadCellData_TexelSize.x;
	uv.y *= _QuadCellData_TexelSize.y;
	return FilterCellData(tex2Dlod(_QuadCellData, float4(uv, 0, 0)));
}