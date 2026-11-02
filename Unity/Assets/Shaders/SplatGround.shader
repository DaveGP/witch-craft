// The island's ground: grass blended with up to four other grounds (forest floor, meadow, moss, earth) by a control
// map, like a terrain's splat map, on the one ground mesh. Each channel of the control map is how much of that layer
// shows; grass fills whatever is left. Textures are projected from above in world space (from the side on steep
// faces), so they tile continuously across the island.
Shader "WitchCraft/SplatGround"
{
	Properties
	{
		[NoScaleOffset] _Control ("Control map (R forest floor, G meadow, B moss, A earth)", 2D) = "black" {}
		_ControlRect ("Control map area (x min, z min, width, depth)", Vector) = (-90, -90, 180, 180)

		_BaseMap ("Grass", 2D) = "white" {}
		_BaseColor ("Grass tint", Color) = (1, 1, 1, 1)
		_Tiling ("Grass tiling (repeats per metre)", Float) = 0.5

		_Layer1 ("Forest floor", 2D) = "white" {}
		_Tint1 ("Forest floor tint", Color) = (1, 1, 1, 1)
		_Tiling1 ("Forest floor tiling", Float) = 0.5

		_Layer2 ("Meadow", 2D) = "white" {}
		_Tint2 ("Meadow tint", Color) = (1, 1, 1, 1)
		_Tiling2 ("Meadow tiling", Float) = 0.5

		_Layer3 ("Moss", 2D) = "white" {}
		_Tint3 ("Moss tint", Color) = (1, 1, 1, 1)
		_Tiling3 ("Moss tiling", Float) = 0.5

		_Layer4 ("Earth", 2D) = "white" {}
		_Tint4 ("Earth tint", Color) = (1, 1, 1, 1)
		_Tiling4 ("Earth tiling", Float) = 0.2

		_Sharpness ("Blend sharpness", Range(1, 4)) = 1.6
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		TEXTURE2D(_Control); SAMPLER(sampler_Control);
		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		TEXTURE2D(_Layer1); SAMPLER(sampler_Layer1);
		TEXTURE2D(_Layer2); SAMPLER(sampler_Layer2);
		TEXTURE2D(_Layer3); SAMPLER(sampler_Layer3);
		TEXTURE2D(_Layer4); SAMPLER(sampler_Layer4);

		CBUFFER_START(UnityPerMaterial)
			float4 _ControlRect;
			float4 _BaseMap_ST;
			half4 _BaseColor;
			float _Tiling;
			float4 _Layer1_ST;
			half4 _Tint1;
			float _Tiling1;
			float4 _Layer2_ST;
			half4 _Tint2;
			float _Tiling2;
			float4 _Layer3_ST;
			half4 _Tint3;
			float _Tiling3;
			float4 _Layer4_ST;
			half4 _Tint4;
			float _Tiling4;
			float _Sharpness;
		CBUFFER_END
		ENDHLSL

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float fog : TEXCOORD2;
			};

			Varyings Vertex(Attributes input)
			{
				Varyings output;

				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.fog = ComputeFogFactor(output.positionCS.z);

				return output;
			}

			// From above on the flat top, from the side on the island's edge
			float2 Projected(float3 position, float3 normal)
			{
				return abs(normal.y) > 0.5 ? position.xz : float2(position.x + position.z, position.y);
			}

			half4 Fragment(Varyings input) : SV_Target
			{
				half3 normal = normalize(input.normalWS);
				float2 uv = Projected(input.positionWS, normal);

				// Only the flat top is painted; the island's sides stay grass
				float2 controlUV = (input.positionWS.xz - _ControlRect.xy) / _ControlRect.zw;
				half4 control = normal.y > 0.5 ? SAMPLE_TEXTURE2D(_Control, sampler_Control, controlUV) : 0;

				// Sharpen the blend a little so layers meet in a soft band rather than a long smear
				control = pow(saturate(control), _Sharpness);

				half total = control.r + control.g + control.b + control.a;
				control /= max(total, 1.0);
				half grass = saturate(1.0 - total);

				half3 albedo =
					SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * _Tiling).rgb * _BaseColor.rgb * grass +
					SAMPLE_TEXTURE2D(_Layer1, sampler_Layer1, uv * _Tiling1).rgb * _Tint1.rgb * control.r +
					SAMPLE_TEXTURE2D(_Layer2, sampler_Layer2, uv * _Tiling2).rgb * _Tint2.rgb * control.g +
					SAMPLE_TEXTURE2D(_Layer3, sampler_Layer3, uv * _Tiling3).rgb * _Tint3.rgb * control.b +
					SAMPLE_TEXTURE2D(_Layer4, sampler_Layer4, uv * _Tiling4).rgb * _Tint4.rgb * control.a;

				Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
				half3 lighting = light.color * saturate(dot(normal, light.direction)) * light.shadowAttenuation + SampleSH(normal);

				return half4(MixFog(albedo * lighting, input.fog), 1);
			}
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			ColorMask 0

			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			float3 _LightDirection;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			float4 Vertex(Attributes input) : SV_POSITION
			{
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

				#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
					positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif

				return positionCS;
			}

			half Fragment() : SV_Target { return 0; }
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode" = "DepthOnly" }
			ColorMask R

			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment

			float4 Vertex(float4 positionOS : POSITION) : SV_POSITION
			{
				return TransformObjectToHClip(positionOS.xyz);
			}

			half Fragment(float4 positionCS : SV_POSITION) : SV_Target
			{
				return positionCS.z;
			}
			ENDHLSL
		}
	}
}
