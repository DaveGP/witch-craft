// A patch of different ground laid over the island: forest floor, meadow, moss, mud, dirt. The texture is projected
// from above in world space, so it lines up with the ground around it, and the edge fades out over a noisy, dithered
// falloff instead of ending in a hard circle. Each patch's edge wobbles differently, seeded by where it stands.
// Drawn on a flattened cylinder (radius 0.5 in object space); opaque with alpha clipping, so it sorts like the ground.
Shader "WitchCraft/GroundPatch"
{
	Properties
	{
		_BaseMap ("Ground texture (seen from above)", 2D) = "white" {}
		_BaseColor ("Tint", Color) = (1, 1, 1, 1)
		_Tiling ("World tiling (repeats per metre)", Float) = 0.5
		_Feather ("Edge softness", Range(0.02, 1)) = 0.5
		_Wobble ("Edge wobble", Range(0, 0.5)) = 0.2
	}

	SubShader
	{
		Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		TEXTURE2D(_BaseMap);
		SAMPLER(sampler_BaseMap);

		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			half4 _BaseColor;
			float _Tiling;
			float _Feather;
			float _Wobble;
		CBUFFER_END

		// 1 in the middle, 0 past the wobbly edge, a soft ramp between
		float Coverage(float2 local)
		{
			float radius = length(local);
			float angle = atan2(local.y, local.x);

			float3 origin = TransformObjectToWorld(float3(0, 0, 0));
			float seed = frac(sin(dot(origin.xz, float2(12.9898, 78.233))) * 43758.5453) * 6.2831;

			float wobble = (sin(angle * 3.0 + seed) * 0.5 + sin(angle * 7.0 + seed * 1.7) * 0.3 + sin(angle * 13.0 + seed * 2.3) * 0.2) * _Wobble;
			float edge = 1.0 - _Wobble + wobble;

			return 1.0 - smoothstep(edge - _Feather, edge, radius);
		}

		// Interleaved gradient noise: a fine, even dither to fade an opaque edge
		float Dither(float2 pixel)
		{
			return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
		}
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
				float2 local : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float fog : TEXCOORD3;
			};

			Varyings Vertex(Attributes input)
			{
				Varyings output;

				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.local = input.positionOS.xz * 2.0;
				output.fog = ComputeFogFactor(output.positionCS.z);

				return output;
			}

			half4 Fragment(Varyings input) : SV_Target
			{
				clip(Coverage(input.local) - Dither(input.positionCS.xy));

				half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.positionWS.xz * _Tiling) * _BaseColor;

				Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
				half3 normal = normalize(input.normalWS);
				half3 lighting = light.color * saturate(dot(normal, light.direction)) * light.shadowAttenuation + SampleSH(normal);

				return half4(MixFog(albedo.rgb * lighting, input.fog), 1);
			}
			ENDHLSL
		}

		// For the depth prepass, with the same edge
		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode" = "DepthOnly" }
			ColorMask R

			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment

			struct Attributes
			{
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 local : TEXCOORD0;
			};

			Varyings Vertex(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.local = input.positionOS.xz * 2.0;
				return output;
			}

			half Fragment(Varyings input) : SV_Target
			{
				clip(Coverage(input.local) - Dither(input.positionCS.xy));
				return input.positionCS.z;
			}
			ENDHLSL
		}
	}
}
