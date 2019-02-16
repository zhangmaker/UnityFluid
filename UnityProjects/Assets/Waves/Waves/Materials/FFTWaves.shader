Shader "Fluid/FFTWaves" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_BottomColor("Bottom Color", Color) = (0,0,0,0)
		_TopColor("Top Color", Color) = (1,1,1,1)
		_WaveLength("Wave Length", Vector) = (1,1,1,1)
		_Amplitude("Amplitude", Vector) = (1,1,1,1)
		_Speed("Speed", Vector) = (1,0.5,0.25,0.125)
		_Direction("Direction", Vector) = (1,0,0,0)
		_SteepnessOfWaves("Steepness of the waves", Vector) = (0.5,0.5,0.5,0.5)			//the value of the parment must between 0 ~ 1
		_CurrentTime("Current Time", Float) = 0
	}

	SubShader {
		Tags {
			"RenderType" = "Transparent"
			"Queue" = "Overlay"
			"IgnoreProjector " = "True"
		}
		LOD 300

		ZWrite On
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
					// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float3 normal : NORMAL;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
			};

			static float PI = 3.1416f;
			static float PI_2 = 6.2832f;

			StructuredBuffer<float3> buf_Points;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			uniform float4 _BottomColor;
			uniform float4 _TopColor;
			uniform float4 _WaveLength;
			uniform float4 _Amplitude;
			uniform float4 _Direction;
			uniform float4 _Speed;
			uniform float4 _SteepnessOfWaves;
			uniform float _CurrentTime;

			v2f vert(appdata v) {
				v2f o;

				float2 pos = float2(v.vertex.x, v.vertex.z);

				float2 dir1 = normalize(float2(cos(PI_2 * _Direction.x), sin(PI_2 * _Direction.x)));
				float2 dir2 = normalize(float2(cos(PI_2 * _Direction.y), sin(PI_2 * _Direction.y)));
				float2 dir3 = normalize(float2(cos(PI_2 * _Direction.z), sin(PI_2 * _Direction.z)));
				float2 dir4 = normalize(float2(cos(PI_2 * _Direction.w), sin(PI_2 * _Direction.w)));

				float dirDotPoint1 = dot(pos, dir1);
				float dirDotPoint2 = dot(pos, dir2);
				float dirDotPoint3 = dot(pos, dir3);
				float dirDotPoint4 = dot(pos, dir4);

				float4 kVec = float4(2 * PI / _WaveLength.x, 2 * PI / _WaveLength.y, 2 * PI / _WaveLength.z, 2 * PI / _WaveLength.w);
				float height = 
					_Amplitude.x*sin(dirDotPoint1*kVec.x + _CurrentTime * _Speed.x) +
					_Amplitude.y*sin(dirDotPoint2*kVec.y + _CurrentTime * _Speed.y) +
					_Amplitude.z*sin(dirDotPoint3*kVec.z + _CurrentTime * _Speed.z) +
					_Amplitude.w*sin(dirDotPoint4*kVec.w + _CurrentTime * _Speed.w);
				
				float offsetX =
					_SteepnessOfWaves.x * dir1.x * cos(dirDotPoint1*kVec.x + _CurrentTime * _Speed.x) +
					_SteepnessOfWaves.y * dir2.x * cos(dirDotPoint2*kVec.y + _CurrentTime * _Speed.y) +
					_SteepnessOfWaves.z * dir3.x * cos(dirDotPoint3*kVec.z + _CurrentTime * _Speed.z) +
					_SteepnessOfWaves.w * dir4.x * cos(dirDotPoint4*kVec.w + _CurrentTime * _Speed.w);
				float offsetY =
					_SteepnessOfWaves.x * dir1.y * cos(dirDotPoint1*kVec.x + _CurrentTime * _Speed.x) +
					_SteepnessOfWaves.y * dir2.y * cos(dirDotPoint2*kVec.y + _CurrentTime * _Speed.y) +
					_SteepnessOfWaves.z * dir3.y * cos(dirDotPoint3*kVec.z + _CurrentTime * _Speed.z) +
					_SteepnessOfWaves.w * dir4.y * cos(dirDotPoint4*kVec.w + _CurrentTime * _Speed.w);

				float3 validObjectPos = float3(v.vertex.x + offsetX, height + v.vertex.y, v.vertex.z + offsetY);
				//float3 validObjectPos = v.vertex;

				float normalX =
					-(kVec.x * dir1.x * _Amplitude.x*cos(dirDotPoint1*kVec.x + _CurrentTime * _Speed.x) +
					kVec.y * dir2.x * _Amplitude.y*cos(dirDotPoint2*kVec.y + _CurrentTime * _Speed.y) +
					kVec.z * dir3.x * _Amplitude.z*cos(dirDotPoint3*kVec.z + _CurrentTime * _Speed.z) +
					kVec.w * dir4.x * _Amplitude.w*cos(dirDotPoint4*kVec.w + _CurrentTime * _Speed.w));
				float normalY =
					1.0f-(_SteepnessOfWaves.x * sin(dirDotPoint1*kVec.x + _CurrentTime * _Speed.x) +
						_SteepnessOfWaves.y * sin(dirDotPoint2*kVec.y + _CurrentTime * _Speed.y) +
						_SteepnessOfWaves.z * sin(dirDotPoint3*kVec.z + _CurrentTime * _Speed.z) +
						_SteepnessOfWaves.w * sin(dirDotPoint4*kVec.w + _CurrentTime * _Speed.w));
				float normalZ =
					-(kVec.x * dir1.y * _Amplitude.x*cos(dirDotPoint1*kVec.x + _CurrentTime * _Speed.x) +
					kVec.y * dir2.y * _Amplitude.y*cos(dirDotPoint2*kVec.y + _CurrentTime * _Speed.y) +
					kVec.z * dir3.y * _Amplitude.z*cos(dirDotPoint3*kVec.z + _CurrentTime * _Speed.z) +
					kVec.w * dir4.y * _Amplitude.w*cos(dirDotPoint4*kVec.w + _CurrentTime * _Speed.w));

				float3 validNormal = normalize(float3(normalX, normalY, normalZ) + v.normal);

				o.vertex = UnityObjectToClipPos(validObjectPos);
				o.color = v.color;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = UnityObjectToWorldNormal(validNormal);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				// sample the texture
				float3 lightDir = normalize(float3(0, 1, 1)); //normalize(_WorldSpaceLightPos0.xyz - i.vertex);
				float3 halfDir = normalize(float3(0, 1, -1)); // normalize(_WorldSpaceCameraPos - i.vertex);
				float4 c = float4(1, 1, 1, 1);//texture(water, tex_coord);

				float4 emissive_color = float4(1.0, 1.0, 1.0, 1.0);
				float4 ambient_color = float4(0.0, 0.65, 0.75, 1.0);
				float4 diffuse_color = float4(0.5, 0.65, 0.75, 1.0);
				float4 specular_color = float4(1.0, 0.25, 0.0, 1.0);

				float emissive_contribution = 0.00;
				float ambient_contribution = 0.30;
				float diffuse_contribution = 0.30;
				float specular_contribution = 1.80;

				float d = dot(i.normal, lightDir);
				bool facing = d > 0.0;

				float4 fragColor = emissive_color * emissive_contribution +
					ambient_color * ambient_contribution  * c +
					diffuse_color * diffuse_contribution  * c * max(d, 0) +
					(facing ? specular_color * specular_contribution * c * max(pow(dot(i.normal, halfDir), 120.0), 0.0) :
						float4(0.0, 0.0, 0.0, 0.0));
				fragColor.a = 0.7f;
				//fragColor = float4(i.vertex.rgb, 0.7f);

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, fragColor);
				return fragColor;
			}
			ENDCG
		}
	}
}
