Shader "Fluid/SinWaves" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_BottomColor("Bottom Color", Color) = (0,0,0,0)
		_TopColor("Top Color", Color) = (1,1,1,1)
		_WaveLength("Wave Length", Vector) = (1,1,1,1)
		_Amplitude("Amplitude", Vector) = (1,1,1,1)
		_Speed("Speed", Vector) = (1,0.5,0.25,0.125)
		_Direction("Direction", Vector) = (1,0,0,0)
	}

	SubShader {
		Tags{
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			uniform float4 _BottomColor;
			uniform float4 _TopColor;
			uniform float4 _WaveLength;
			uniform float4 _Amplitude;
			uniform float4 _Direction;
			uniform float4 _Speed;
			
			v2f vert (appdata v) {
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
					_Amplitude.x*sin(dirDotPoint1*kVec.x + _Time.y * _Speed.x) +
					_Amplitude.y*sin(dirDotPoint2*kVec.y + _Time.y * _Speed.y) +
					_Amplitude.z*sin(dirDotPoint3*kVec.z + _Time.y * _Speed.z) +
					_Amplitude.w*sin(dirDotPoint4*kVec.w + _Time.y * _Speed.w);
				float3 validObjectPos = float3(v.vertex.x, height, v.vertex.z);

				float partialDifferentiationOfHOnX = 
					kVec.x * dir1.x * _Amplitude.x*cos(dirDotPoint1*kVec.x + _Time.y * _Speed.x) +
					kVec.y * dir2.x * _Amplitude.y*cos(dirDotPoint2*kVec.y + _Time.y * _Speed.y) +
					kVec.z * dir3.x * _Amplitude.z*cos(dirDotPoint3*kVec.z + _Time.y * _Speed.z) +
					kVec.w * dir4.x * _Amplitude.w*cos(dirDotPoint4*kVec.w + _Time.y * _Speed.w);
				float partialDifferentiationOfHOnY =
					kVec.x * dir1.y * _Amplitude.x*cos(dirDotPoint1*kVec.x + _Time.y * _Speed.x) +
					kVec.y * dir2.y * _Amplitude.y*cos(dirDotPoint2*kVec.y + _Time.y * _Speed.y) +
					kVec.z * dir3.y * _Amplitude.z*cos(dirDotPoint3*kVec.z + _Time.y * _Speed.z) +
					kVec.w * dir4.y * _Amplitude.w*cos(dirDotPoint4*kVec.w + _Time.y * _Speed.w);

				o.vertex = UnityObjectToClipPos(validObjectPos);
				o.color = lerp(_BottomColor, _TopColor, smoothstep( 0 , 1 , (v.vertex.y + 0.5f) / 1.0f));
				//o.color = float4(height, 0, 0, 1);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = UnityObjectToWorldNormal(float3(-partialDifferentiationOfHOnX, -partialDifferentiationOfHOnY, 1));
				
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 col = float4(i.color.rgb, 0.5f);

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
