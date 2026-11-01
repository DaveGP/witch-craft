using UnityEngine;

namespace Home
{
	// The surfaces of a built piece that take paint: walls, boards, thatch and fabric, but not trims, glass or lights.
	public class Paintable : MonoBehaviour
	{
		private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

		[SerializeField] private Renderer[] surfaces = new Renderer[0];

		public int Colour { get; private set; }

		private MaterialPropertyBlock block;

		public void SetSurfaces(Renderer[] renderers)
		{
			surfaces = renderers;
		}

		public void Paint(int colour)
		{
			Colour = HomePalette.Clamp(colour);

			Apply();
		}

		/// <summary>
		/// Puts the paint back after a highlight (levitating, unmaking) has cleared it.
		/// </summary>
		public void Apply()
		{
			block ??= new MaterialPropertyBlock();

			foreach (var surface in surfaces)
			{
				if (surface == null)
					continue;

				if (Colour == 0)
				{
					surface.SetPropertyBlock(null);
					continue;
				}

				var original = surface.sharedMaterial != null && surface.sharedMaterial.HasProperty(BaseColor)
					? surface.sharedMaterial.GetColor(BaseColor)
					: Color.white;

				// Keep a little of the material's own tone so painted wood still reads as wood
				surface.GetPropertyBlock(block);
				block.SetColor(BaseColor, Color.Lerp(original, HomePalette.Entries[Colour].Colour, 0.82f));
				surface.SetPropertyBlock(block);
			}
		}
	}
}
