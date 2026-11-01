using System.Linq;
using UnityEditor;
using UnityEngine;


namespace WitchCraftEditor
{
	// Converts a pack's Built-in materials (Standard, Standard (Specular setup), Legacy Diffuse...) to URP Lit in
	// place, carrying over textures, colours and the surface mode. Materials already on a URP shader, or on a
	// pack's own custom shader, are left alone. Safe to run again.
	public static class UrpMaterialConverter
	{
		private const string LitShader = "Universal Render Pipeline/Lit";

		/// <summary>
		/// Converts every material under a folder. Returns how many were changed.
		/// </summary>
		public static int ConvertFolder(string folder)
		{
			var converted = 0;

			foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
			{
				var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));

				if (material != null && Convert(material))
					converted++;
			}

			if (converted > 0)
				AssetDatabase.SaveAssets();

			return converted;
		}

		public static bool NeedsConversion(Material material)
		{
			if (material == null || material.shader == null)
				return false;

			var shader = material.shader.name;

			// A missing shader shows as the error shader; built-in ones are Standard and the Legacy family
			return shader == "Hidden/InternalErrorShader" || shader.StartsWith("Standard") || shader.StartsWith("Legacy Shaders/")
				|| shader is "Diffuse" or "Bumped Diffuse" or "Specular" or "Bumped Specular" or "Transparent/Diffuse" or "Transparent/Cutout/Diffuse";
		}

		public static bool Convert(Material material)
		{
			if (!NeedsConversion(material))
				return false;

			var lit = Shader.Find(LitShader);

			if (lit == null)
			{
				Debug.LogError($"{nameof(UrpMaterialConverter)}: {LitShader} not found");
				return false;
			}

			var oldShader = material.shader.name;

			// Read everything before the shader swap clears what the new shader doesn't declare
			var mainTexture = Texture(material, "_MainTex");
			var scale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
			var offset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
			var colour = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
			var normal = Texture(material, "_BumpMap");
			var normalScale = Float(material, "_BumpScale", 1f);
			var metallicMap = Texture(material, "_MetallicGlossMap");
			var metallic = Float(material, "_Metallic", 0f);
			var smoothness = Float(material, "_Glossiness", Float(material, "_Shininess", 0.5f));
			var occlusion = Texture(material, "_OcclusionMap");
			var emissionMap = Texture(material, "_EmissionMap");
			var emission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
			var cutoff = Float(material, "_Cutoff", 0.5f);
			var emissive = material.IsKeywordEnabled("_EMISSION") && (emission.maxColorComponent > 0.01f || emissionMap != null);

			// Standard's _Mode: 0 opaque, 1 cutout, 2 fade, 3 transparent. Legacy shaders say it in their name
			var mode = (int)Float(material, "_Mode", 0f);

			if (oldShader.Contains("Cutout"))
				mode = 1;
			else if (oldShader.Contains("Transparent"))
				mode = 3;

			var queue = material.renderQueue;

			material.shader = lit;

			material.SetTexture("_BaseMap", mainTexture);
			material.SetTextureScale("_BaseMap", scale);
			material.SetTextureOffset("_BaseMap", offset);
			material.SetColor("_BaseColor", colour);
			material.SetFloat("_Metallic", metallic);
			material.SetFloat("_Smoothness", smoothness);

			if (normal != null)
			{
				material.SetTexture("_BumpMap", normal);
				material.SetFloat("_BumpScale", normalScale);
				material.EnableKeyword("_NORMALMAP");
			}

			if (metallicMap != null)
			{
				material.SetTexture("_MetallicGlossMap", metallicMap);
				material.EnableKeyword("_METALLICSPECGLOSSMAP");
			}

			if (occlusion != null)
			{
				material.SetTexture("_OcclusionMap", occlusion);
				material.EnableKeyword("_OCCLUSIONMAP");
			}

			if (emissive)
			{
				material.SetColor("_EmissionColor", emission);
				material.SetTexture("_EmissionMap", emissionMap);
				material.EnableKeyword("_EMISSION");
				material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
			}
			else
			{
				material.SetColor("_EmissionColor", Color.black);
				material.DisableKeyword("_EMISSION");
			}

			SetSurface(material, mode, cutoff);

			if (mode == 0 && queue > 0 && queue < 2450)
				material.renderQueue = -1;

			EditorUtility.SetDirty(material);

			Debug.Log($"{nameof(UrpMaterialConverter)}: {material.name} {oldShader} → URP Lit");

			return true;
		}

		private static void SetSurface(Material material, int mode, float cutoff)
		{
			var transparent = mode >= 2;
			var clip = mode == 1;

			material.SetFloat("_Surface", transparent ? 1f : 0f);
			material.SetFloat("_Blend", 0f);
			material.SetFloat("_AlphaClip", clip ? 1f : 0f);
			material.SetFloat("_Cutoff", cutoff);
			material.SetFloat("_ZWrite", transparent ? 0f : 1f);
			material.SetFloat("_SrcBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
			material.SetFloat("_DstBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);

			if (clip)
				material.EnableKeyword("_ALPHATEST_ON");
			else
				material.DisableKeyword("_ALPHATEST_ON");

			if (transparent)
			{
				material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
				material.SetOverrideTag("RenderType", "Transparent");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
			}
			else
			{
				material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
				material.SetOverrideTag("RenderType", clip ? "TransparentCutout" : "Opaque");
				material.renderQueue = clip ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
			}

			material.SetShaderPassEnabled("ShadowCaster", !transparent);
		}

		private static Texture Texture(Material material, string property)
			=> material.HasProperty(property) ? material.GetTexture(property) : null;

		private static float Float(Material material, string property, float fallback)
			=> material.HasProperty(property) ? material.GetFloat(property) : fallback;

		[MenuItem("Assets/Witch Craft/Convert Materials to URP Lit", false, 2000)]
		private static void ConvertSelection()
		{
			var converted = 0;

			foreach (var path in Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath))
			{
				if (AssetDatabase.IsValidFolder(path))
					converted += ConvertFolder(path);
				else if (Convert(AssetDatabase.LoadAssetAtPath<Material>(path)))
					converted++;
			}

			AssetDatabase.SaveAssets();

			Debug.Log($"{nameof(UrpMaterialConverter)}: converted {converted} materials.");
		}
	}
}
