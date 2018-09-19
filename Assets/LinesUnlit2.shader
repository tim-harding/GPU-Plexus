Shader "Custom/Grid"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_Thickness("Thickness", Float) = 1
		_MaxDistance("Max Distance", Float) = 1
		_ConvergeDistanceModifier("Converge Distance Modifier", Float) = 1
	}

	SubShader
	{
		Tags 
		{ 
			"RenderType" = "Transparent" 
			"Queue"="Transparent" 
		}

		Pass
		{
			Blend One One 
			ZTest Always
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			#include "UnityCG.cginc"

			float4 _Color;
			float _Thickness;
			float _MaxDistance;
			float _ConvergeDistanceModifier;

			int3 _Dimensions;
			float3 _Converge;

			StructuredBuffer<float3> _Positions;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2g
			{
				float4 vertex : POSITION;
			};

			struct g2f 
			{
				float4 vertex : POSITION;
				float2 data : TEXCOORD0; // u, distance to neighbor
			};

			v2g vert (appdata v)
			{
				v2g o;
				o.vertex = v.vertex;
				return o;
			}

			void addLine(float3 source, float3 destination, inout TriangleStream<g2f> stream) 
			{
				float4 p1 = UnityObjectToClipPos(float4(source, 1));
				float4 p2 = UnityObjectToClipPos(float4(destination, 1));

				float2 dir = normalize(p2.xy - p1.xy);
				float2 normal = float2(-dir.y, dir.x);

				float4 offset1 = float4(normal * p1.w * _Thickness, 0, 0);
				float4 offset2 = float4(normal * p2.w * _Thickness, 0, 0);

				float4 o1 = p1 + offset1;
				float4 o2 = p1 - offset1;
				float4 o3 = p2 + offset2;
				float4 o4 = p2 - offset2;

				g2f g[4];
				float convergeDistance = 1 - 1 / (length(source - _Converge) / _ConvergeDistanceModifier + 1);
				float distance = length(source - destination) * convergeDistance;

				g[0].vertex = o1;
				g[0].data = float2(1, distance);
				g[1].vertex = o2;
				g[1].data = float2(0, distance);
				g[2].vertex = o3;
				g[2].data = float2(1, distance);
				g[3].vertex = o4;
				g[3].data = float2(0, distance);

				stream.Append(g[0]);
				stream.Append(g[1]);
				stream.Append(g[2]);
				stream.Append(g[3]);
				stream.RestartStrip();
			}

			float3 positionForCell(int3 cell) 
			{
				int index = cell.x * _Dimensions.z * _Dimensions.y + cell.y * _Dimensions.z + cell.z;
				return _Positions[index];
			}

			float3 positionForCellOffset(int3 cell, int x, int y, int z) 
			{
				int3 offset = int3(x, y, z);
				int3 target = cell + offset;
				bool valid = 
					target.x > 0 && target.x < _Dimensions.x && 
					target.y > 0 && target.y < _Dimensions.y && 
					target.z > 0 && target.z < _Dimensions.z;
				target = cell + offset * valid;
				return positionForCell(target);
			}

			// Construct lines connecting this cell to each of its neighbors on
			// half of the hemisphere, to prevent duplicate connections.
			[maxvertexcount(4 * 12)]
			void geom(point v2g input[1], inout TriangleStream<g2f> stream)
			{
				float4 vertex = input[0].vertex;
				int3 cell = (int3)vertex.xyz;
				float3 position = positionForCell(cell);
				addLine(position, positionForCellOffset(cell, 1,  1,  0), stream);
				addLine(position, positionForCellOffset(cell, 1,  0,  0), stream);
				addLine(position, positionForCellOffset(cell, 1, -1,  0), stream);
				addLine(position, positionForCellOffset(cell, 0, -1,  0), stream);
				addLine(position, positionForCellOffset(cell, 1,  1,  1), stream);
				addLine(position, positionForCellOffset(cell, 1,  0,  1), stream);
				addLine(position, positionForCellOffset(cell, 1, -1,  1), stream);
				addLine(position, positionForCellOffset(cell, 0, -1,  1), stream);
				addLine(position, positionForCellOffset(cell, 1,  1, -1), stream);
				addLine(position, positionForCellOffset(cell, 1,  0, -1), stream);
				addLine(position, positionForCellOffset(cell, 1, -1, -1), stream);
				addLine(position, positionForCellOffset(cell, 0, -1, -1), stream);
			}

			fixed4 frag (g2f i) : SV_Target
			{
				float distance = clamp(_MaxDistance - i.data.y, 0, 1);
				return _Color * (1 - abs(i.data.x - 0.5) * 2) * distance * distance;
			}
			ENDCG
		}
	}
}
