using UnityEngine;

namespace Utilities
{
	// Tints every renderer under a root without instancing materials, and can put them back.
	public static class RendererTint
	{
		private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

		private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

		private static MaterialPropertyBlock block;

		public static void Apply(GameObject root, Color tint, float glow = 0f)
		{
			block ??= new MaterialPropertyBlock();

			foreach (var renderer in root.GetComponentsInChildren<Renderer>())
			{
				if (renderer is ParticleSystemRenderer)
					continue;

				renderer.GetPropertyBlock(block);

				var baseColor = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColor)
					? renderer.sharedMaterial.GetColor(BaseColor)
					: Color.white;

				block.SetColor(BaseColor, Color.Lerp(baseColor, tint, 0.55f));
				block.SetColor(EmissionColor, tint * glow);

				renderer.SetPropertyBlock(block);
			}
		}

		public static void Clear(GameObject root)
		{
			foreach (var renderer in root.GetComponentsInChildren<Renderer>())
				renderer.SetPropertyBlock(null);
		}

		public static void SetMaterial(GameObject root, Material material)
		{
			foreach (var renderer in root.GetComponentsInChildren<Renderer>())
			{
				var materials = renderer.sharedMaterials;

				for (var i = 0; i < materials.Length; i++)
					materials[i] = material;

				renderer.sharedMaterials = materials;
			}
		}
	}
}
