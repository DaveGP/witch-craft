using System.Collections.Generic;
using System.IO;
using System.Linq;
using Art;
using Data.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace WitchCraftEditor
{
	// Shared helpers for the content and world builders: generated materials, meshes and primitive assembly.
	public static class WitchCraftBuildKit
	{
		public const string MaterialFolder = "Assets/Materials/Generated";

		public const string MeshFolder = "Assets/Meshes/Generated";

		public const string TextureFolder = "Assets/Textures/Generated";

		private static readonly Dictionary<string, Material> materials = new();

		public static int Ground => LayerMask.NameToLayer("Ground");

		public static int EnvironmentLayer => LayerMask.NameToLayer("Environment");

		public static int Interactable => LayerMask.NameToLayer("Interactable");

		public static int Player => LayerMask.NameToLayer("Player");

		public static void EnsureFolder(string path)
		{
			if (AssetDatabase.IsValidFolder(path))
				return;

			var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
			EnsureFolder(parent);
			AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
		}

		public static T LoadOrCreate<T>(string path) where T : ScriptableObject
		{
			var asset = AssetDatabase.LoadAssetAtPath<T>(path);

			if (asset != null)
				return asset;

			EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));

			asset = ScriptableObject.CreateInstance<T>();
			AssetDatabase.CreateAsset(asset, path);

			return asset;
		}

		#region Materials

		public static Material Lit(string name, Color colour, float smoothness = 0.15f, Color? emission = null)
		{
			var material = GetMaterial(name, "Universal Render Pipeline/Lit");

			material.SetColor("_BaseColor", colour);
			material.SetFloat("_Smoothness", smoothness);
			material.SetFloat("_Metallic", 0f);

			if (emission.HasValue)
			{
				material.EnableKeyword("_EMISSION");
				material.SetColor("_EmissionColor", emission.Value);
				material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
			}

			EditorUtility.SetDirty(material);

			return material;
		}

		/// <summary>
		/// Ground that blends grass with up to four other grounds by a control map (WitchCraft/SplatGround).
		/// </summary>
		public static Material SplatGround(string name, Color grass, params Color[] layers)
		{
			var material = GetMaterial(name, "WitchCraft/SplatGround");

			material.SetColor("_BaseColor", grass);

			for (var i = 0; i < layers.Length && i < 4; i++)
				material.SetColor($"_Tint{i + 1}", layers[i]);

			EditorUtility.SetDirty(material);

			return material;
		}

		/// <summary>
		/// Saves a painted control map: linear (it holds weights, not colours), clamped, and uncompressed so the
		/// blends stay smooth.
		/// </summary>
		public static Texture2D SaveControlMap(string name, Color[] pixels, int size)
		{
			EnsureFolder(TextureFolder);

			var path = $"{TextureFolder}/{name}.png";

			var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
			texture.SetPixels(pixels);
			File.WriteAllBytes(path, texture.EncodeToPNG());
			Object.DestroyImmediate(texture);

			AssetDatabase.ImportAsset(path);

			var importer = (TextureImporter)AssetImporter.GetAtPath(path);
			importer.sRGBTexture = false;
			importer.alphaIsTransparency = false;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.mipmapEnabled = true;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.SaveAndReimport();

			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		public static Material Unlit(string name, Color colour, bool transparent = false)
		{
			var material = GetMaterial(name, "Universal Render Pipeline/Unlit");

			material.SetColor("_BaseColor", colour);

			if (transparent)
				MakeTransparent(material);

			EditorUtility.SetDirty(material);

			return material;
		}

		public static Material Transparent(string name, Color colour, float smoothness = 0.8f)
		{
			var material = Lit(name, colour, smoothness);

			MakeTransparent(material);

			return material;
		}

		public static Material Particle(string name, Texture2D texture)
		{
			var material = GetMaterial(name, "Universal Render Pipeline/Particles/Unlit");

			material.SetTexture("_BaseMap", texture);
			material.SetColor("_BaseColor", Color.white);

			MakeTransparent(material);

			EditorUtility.SetDirty(material);

			return material;
		}

		public static Material Skybox(string name)
		{
			var material = GetMaterial(name, "Skybox/Procedural");

			material.SetColor("_SkyTint", new Color(0.62f, 0.6f, 0.9f));
			material.SetColor("_GroundColor", new Color(0.45f, 0.5f, 0.6f));
			material.SetFloat("_AtmosphereThickness", 0.75f);
			material.SetFloat("_Exposure", 1.25f);
			material.SetFloat("_SunSize", 0.05f);

			EditorUtility.SetDirty(material);

			return material;
		}

		private static void MakeTransparent(Material material)
		{
			material.SetFloat("_Surface", 1f);
			material.SetFloat("_Blend", 0f);
			material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
			material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
			material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
			material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
			material.SetFloat("_ZWrite", 0f);
			material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material.SetOverrideTag("RenderType", "Transparent");
			material.renderQueue = (int)RenderQueue.Transparent;
		}

		private static Material GetMaterial(string name, string shaderName)
		{
			if (materials.TryGetValue(name, out var cached) && cached != null)
				return cached;

			EnsureFolder(MaterialFolder);

			var path = $"{MaterialFolder}/{name}.mat";

			var material = AssetDatabase.LoadAssetAtPath<Material>(path);

			var shader = Shader.Find(shaderName);

			if (material == null)
			{
				material = new Material(shader);
				AssetDatabase.CreateAsset(material, path);
			}
			else if (material.shader != shader)
			{
				material.shader = shader;
			}

			materials[name] = material;

			return material;
		}

		public static Texture2D SoftDot()
		{
			EnsureFolder(TextureFolder);

			var path = $"{TextureFolder}/SoftDot.png";

			if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
			{
				const int size = 64;

				var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

				for (var y = 0; y < size; y++)
				for (var x = 0; x < size; x++)
				{
					var distance = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f - 0.5f, size / 2f - 0.5f)) / (size / 2f);
					var alpha = Mathf.Clamp01(1f - distance);
					texture.SetPixel(x, y, new Color(1, 1, 1, alpha * alpha));
				}

				File.WriteAllBytes(path, texture.EncodeToPNG());
				Object.DestroyImmediate(texture);

				AssetDatabase.ImportAsset(path);

				var importer = (TextureImporter)AssetImporter.GetAtPath(path);
				importer.alphaIsTransparency = true;
				importer.mipmapEnabled = false;
				importer.SaveAndReimport();
			}

			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		public static void ClearCache() => materials.Clear();

		#endregion

		#region Meshes

		public static Mesh Cone() => SaveMesh("Cone", BuildCone(20));

		public static Mesh Wedge() => SaveMesh("Wedge", BuildWedge());

		public static Mesh WedgeShell() => SaveMesh("WedgeShell", BuildWedgeShell());

		public static Mesh HillMesh() => ReplaceMesh("Hill", BuildHill(28, 48));

		public static Mesh RockyUnderside(int seed) => ReplaceMesh($"RockyUnderside{seed}", BuildRockyUnderside(seed));

		public static Mesh CaveShell() => ReplaceMesh("CaveShell", BuildCaveShell(new System.Random(5)));

		// Saves a generated mesh over the last build's, for shapes that are still being tuned
		private static Mesh ReplaceMesh(string name, Mesh mesh)
		{
			EnsureFolder(MeshFolder);

			var path = $"{MeshFolder}/{name}.asset";
			var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

			if (existing == null)
			{
				mesh.name = name;
				AssetDatabase.CreateAsset(mesh, path);
				return mesh;
			}

			existing.Clear();
			existing.SetVertices(mesh.vertices);
			existing.SetTriangles(mesh.triangles, 0);
			existing.SetNormals(mesh.normals);
			existing.RecalculateBounds();
			EditorUtility.SetDirty(existing);
			Object.DestroyImmediate(mesh);

			return existing;
		}

		// A hill of radius 1 and height 1: a smooth bell that rises from level ground and rounds over the top, so
		// walking on and off it is seamless. Its rim tucks just under the ground. Smooth-shaded
		private static Mesh BuildHill(int rings, int segments)
		{
			var vertices = new List<Vector3> { new(0f, 1f, 0f) };
			var triangles = new List<int>();

			Vector3 Ring(float r, float h, int s)
			{
				var angle = s * Mathf.PI * 2f / segments;
				return new Vector3(Mathf.Cos(angle) * r, h, Mathf.Sin(angle) * r);
			}

			for (var i = 1; i <= rings; i++)
			{
				var r = i / (float)rings;
				var h = (Mathf.Cos(Mathf.PI * r) + 1f) / 2f;

				for (var s = 0; s < segments; s++)
					vertices.Add(Ring(r, h, s));
			}

			for (var s = 0; s < segments; s++)
				vertices.Add(Ring(1.04f, -0.03f, s));

			int At(int ring, int s) => 1 + (ring - 1) * segments + (s % segments);

			for (var s = 0; s < segments; s++)
			{
				triangles.Add(0);
				triangles.Add(At(1, s + 1));
				triangles.Add(At(1, s));
			}

			for (var ring = 1; ring <= rings; ring++)
			for (var s = 0; s < segments; s++)
			{
				triangles.Add(At(ring, s));
				triangles.Add(At(ring, s + 1));
				triangles.Add(At(ring + 1, s + 1));

				triangles.Add(At(ring, s));
				triangles.Add(At(ring + 1, s + 1));
				triangles.Add(At(ring + 1, s));
			}

			return FaceOut(Finish(vertices, triangles), Vector3.up);
		}

		// The rock torn out beneath a floating isle: radius 1 at the top (y 0), narrowing in jagged, uneven rings to a
		// point about 1 below, with a few stalactite teeth hanging lower. Faceted, like the rest of the low-poly world
		private static Mesh BuildRockyUnderside(int seed)
		{
			var random = new System.Random(seed);
			float Roll(float min, float max) => min + (float)random.NextDouble() * (max - min);

			const int segments = 12;
			const int rings = 6;

			var grid = new Vector3[rings + 1, segments];

			for (var ring = 0; ring <= rings; ring++)
			{
				var t = ring / (float)rings;
				var twist = Roll(-0.2f, 0.2f);

				for (var s = 0; s < segments; s++)
				{
					var angle = (s + twist) * Mathf.PI * 2f / segments;
					var radius = Mathf.Pow(1f - t * 0.92f, 0.8f) * (ring == 0 ? Roll(0.94f, 1f) : Roll(0.72f, 1.12f));
					var y = ring == 0 ? 0f : -t + Roll(-0.06f, 0.06f);

					// Teeth: some points of the lower rings pulled down
					if (ring >= rings - 2 && random.NextDouble() < 0.3)
						y -= Roll(0.15f, 0.45f);

					grid[ring, s] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
				}
			}

			var tip = new Vector3(Roll(-0.1f, 0.1f), -1.25f, Roll(-0.1f, 0.1f));

			var vertices = new List<Vector3>();
			var triangles = new List<int>();

			void Face(Vector3 a, Vector3 b, Vector3 c)
			{
				// Outward from the rock's axis, so the faces are seen from outside
				var centre = (a + b + c) / 3f;
				var normal = Vector3.Cross(b - a, c - a);

				if (Vector3.Dot(normal, new Vector3(centre.x, 0f, centre.z)) < 0f)
					(b, c) = (c, b);

				AddTriangle(vertices, triangles, a, b, c);
			}

			for (var ring = 0; ring < rings; ring++)
			for (var s = 0; s < segments; s++)
			{
				var next = (s + 1) % segments;
				Face(grid[ring, s], grid[ring, next], grid[ring + 1, next]);
				Face(grid[ring, s], grid[ring + 1, next], grid[ring + 1, s]);
			}

			for (var s = 0; s < segments; s++)
				Face(grid[rings, s], grid[rings, (s + 1) % segments], tip);

			// A lid, under the isle's grass, so nothing shows through from the side
			for (var s = 0; s < segments; s++)
				AddTriangle(vertices, triangles, Vector3.zero, grid[0, (s + 1) % segments], grid[0, s]);

			return Finish(vertices, triangles);
		}

		// A cave in a rocky mound: a jagged arched tunnel running along +x from its mouth at x 0 to a rounded back
		// at x 17, about 8 m wide and 5.5 m high inside, under 2.5 to 4 m of rock. Faceted. The inside faces inwards
		// and the outside outwards, so it reads from both, and both sides collide
		private static Mesh BuildCaveShell(System.Random random)
		{
			float Roll(float min, float max) => min + (float)random.NextDouble() * (max - min);

			const int rings = 10;
			const int around = 12;
			const float length = 17f;
			const float width = 4.2f;
			const float height = 5.5f;

			var inner = new Vector3[rings + 1, around + 1];
			var outer = new Vector3[rings + 1, around + 1];

			for (var r = 0; r <= rings; r++)
			{
				// The tunnel narrows into a rounded back
				var shrink = r == rings ? 0.5f : r == rings - 1 ? 0.82f : 1f;
				var x = length * r / rings;

				for (var j = 0; j <= around; j++)
				{
					// From just under the ground on one side, over the top, to under the ground on the other
					var angle = -0.12f + (Mathf.PI + 0.24f) * j / around;
					var bump = r == 0 ? Roll(-0.03f, 0.05f) : Roll(-0.1f, 0.12f);
					var thick = Roll(2.5f, 4f);
					var along = r == 0 || r == rings ? 0f : Roll(-0.4f, 0.4f);

					inner[r, j] = new Vector3(x + along, Mathf.Sin(angle) * height * shrink * (1f + bump), Mathf.Cos(angle) * width * shrink * (1f + bump));
					outer[r, j] = new Vector3(x + along * 1.5f, Mathf.Sin(angle) * (height * shrink + thick) + Roll(-0.4f, 0.4f), Mathf.Cos(angle) * (width * shrink + thick));
				}
			}

			var innerTip = new Vector3(length + 1f, height * 0.3f, 0f);
			var outerTip = new Vector3(length + 4f, height * 0.5f, 0f);

			var vertices = new List<Vector3>();
			var triangles = new List<int>();

			// Faces towards (or away from) a point on the tunnel's axis
			void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 towards, bool inward)
			{
				var normal = Vector3.Cross(b - a, c - a);
				var toPoint = towards - (a + b + c) / 3f;

				if (Vector3.Dot(normal, toPoint) > 0f != inward)
					(b, c) = (c, b);

				AddTriangle(vertices, triangles, a, b, c);
			}

			Vector3 Axis(Vector3 point) => new(point.x, 1f, 0f);

			for (var r = 0; r < rings; r++)
			for (var j = 0; j < around; j++)
			{
				Face(inner[r, j], inner[r + 1, j], inner[r + 1, j + 1], Axis(inner[r, j]), true);
				Face(inner[r, j], inner[r + 1, j + 1], inner[r, j + 1], Axis(inner[r, j]), true);
				Face(outer[r, j], outer[r + 1, j], outer[r + 1, j + 1], Axis(outer[r, j]), false);
				Face(outer[r, j], outer[r + 1, j + 1], outer[r, j + 1], Axis(outer[r, j]), false);
			}

			for (var j = 0; j < around; j++)
			{
				Face(inner[rings, j], inner[rings, j + 1], innerTip, new Vector3(length - 3f, 1f, 0f), true);
				Face(outer[rings, j], outer[rings, j + 1], outerTip, new Vector3(length - 3f, 1f, 0f), false);

				// The mouth's rim, facing out of the tunnel
				var outside = new Vector3(-10f, 1f, 0f);
				Face(inner[0, j], outer[0, j], outer[0, j + 1], outside, true);
				Face(inner[0, j], outer[0, j + 1], inner[0, j + 1], outside, true);
			}

			return Finish(vertices, triangles);
		}

		// Flips a mesh whose first normal points away from the given side
		private static Mesh FaceOut(Mesh mesh, Vector3 side)
		{
			if (Vector3.Dot(mesh.normals[0], side) >= 0f)
				return mesh;

			var triangles = mesh.triangles;

			for (var i = 0; i < triangles.Length; i += 3)
				(triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);

			mesh.triangles = triangles;
			mesh.RecalculateNormals();

			return mesh;
		}

		private static Mesh SaveMesh(string name, Mesh mesh)
		{
			EnsureFolder(MeshFolder);

			var path = $"{MeshFolder}/{name}.asset";

			var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

			if (existing != null)
				return existing;

			mesh.name = name;
			AssetDatabase.CreateAsset(mesh, path);

			return mesh;
		}

		// Unit cone, base radius 0.5 at y 0, tip at y 1
		private static Mesh BuildCone(int segments)
		{
			var vertices = new List<Vector3>();
			var triangles = new List<int>();

			for (var i = 0; i < segments; i++)
			{
				var a0 = i / (float)segments * Mathf.PI * 2f;
				var a1 = (i + 1) / (float)segments * Mathf.PI * 2f;

				var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
				var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);

				AddTriangle(vertices, triangles, p0, Vector3.up, p1);
				AddTriangle(vertices, triangles, p1, Vector3.zero, p0);
			}

			return Finish(vertices, triangles);
		}

		// Roof slope for one 2x2 cell: low along local -Z, rising to a 1.2 high ridge along +Z, with a little overhang
		private static Mesh BuildWedge()
		{
			var vertices = new List<Vector3>();
			var triangles = new List<int>();

			const float half = 1.15f;
			const float height = 1.2f;

			var frontLeft = new Vector3(-half, 0, -half);
			var frontRight = new Vector3(half, 0, -half);
			var backLeft = new Vector3(-half, 0, 1f);
			var backRight = new Vector3(half, 0, 1f);
			var ridgeLeft = new Vector3(-half, height, 1f);
			var ridgeRight = new Vector3(half, height, 1f);

			AddQuad(vertices, triangles, frontLeft, ridgeLeft, ridgeRight, frontRight);
			AddQuad(vertices, triangles, backRight, ridgeRight, ridgeLeft, backLeft);
			AddQuad(vertices, triangles, frontLeft, frontRight, backRight, backLeft);
			AddTriangle(vertices, triangles, frontLeft, backLeft, ridgeLeft);
			AddTriangle(vertices, triangles, frontRight, ridgeRight, backRight);

			return Finish(vertices, triangles);
		}

		// Everything of the wedge but its slope, a hair inside it: the gable ends, the ceiling underneath and the back.
		// Roof art only replaces the slope, so this stays to close the gables and keep the sky (and rain) out
		private static Mesh BuildWedgeShell()
		{
			var vertices = new List<Vector3>();
			var triangles = new List<int>();

			const float half = 1.14f;
			const float height = 1.18f;
			const float front = -1.14f;
			const float back = 0.99f;
			const float floor = 0.01f;

			var frontLeft = new Vector3(-half, floor, front);
			var frontRight = new Vector3(half, floor, front);
			var backLeft = new Vector3(-half, floor, back);
			var backRight = new Vector3(half, floor, back);
			var ridgeLeft = new Vector3(-half, height, back);
			var ridgeRight = new Vector3(half, height, back);

			AddQuad(vertices, triangles, backRight, ridgeRight, ridgeLeft, backLeft);
			AddQuad(vertices, triangles, frontLeft, frontRight, backRight, backLeft);

			// Seen from inside the hut too
			AddQuad(vertices, triangles, backLeft, backRight, frontRight, frontLeft);

			// The gable's top edge sags like the curved roof art, rising slowly from the eave and steeply to the
			// ridge, so it tucks under the roof rather than poking out above it
			const int steps = 8;

			foreach (var x in new[] { -half, half })
			{
				var foot = new Vector3(x, floor, back);

				for (var i = 0; i < steps; i++)
				{
					var a = Sag(x, i / (float)steps);
					var b = Sag(x, (i + 1) / (float)steps);

					AddTriangle(vertices, triangles, foot, a, b);
					AddTriangle(vertices, triangles, foot, b, a);
				}
			}

			Vector3 Sag(float x, float t) => new(x, floor + (height - floor) * Mathf.Pow(t, 1.6f), Mathf.Lerp(front, back, t));

			return Finish(vertices, triangles);
		}

		private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
		{
			var start = vertices.Count;
			vertices.Add(a);
			vertices.Add(b);
			vertices.Add(c);
			triangles.Add(start);
			triangles.Add(start + 1);
			triangles.Add(start + 2);
		}

		private static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
		{
			AddTriangle(vertices, triangles, a, b, c);
			AddTriangle(vertices, triangles, a, c, d);
		}

		private static Mesh Finish(List<Vector3> vertices, List<int> triangles)
		{
			var mesh = new Mesh();
			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();

			return mesh;
		}

		#endregion

		#region Assembly

		public static GameObject Node(string name, Transform parent, Vector3 localPosition = default, float yaw = 0f)
		{
			var node = new GameObject(name);
			node.transform.SetParent(parent, false);
			node.transform.localPosition = localPosition;
			node.transform.localRotation = Quaternion.Euler(0, yaw, 0);

			return node;
		}

		public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool collider = true, Vector3? euler = null)
		{
			var prim = GameObject.CreatePrimitive(type);
			prim.name = name;
			prim.transform.SetParent(parent, false);
			prim.transform.localPosition = localPosition;
			prim.transform.localScale = localScale;
			prim.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);

			prim.GetComponent<Renderer>().sharedMaterial = material;

			if (!collider)
				Object.DestroyImmediate(prim.GetComponent<Collider>());
			else
				MatchColliderToShape(prim, type, localScale);

			if (parent != null)
				prim.layer = parent.gameObject.layer;

			return prim;
		}

		// A cylinder primitive collides as a capsule, and a squashed one becomes a ball as wide as the disc: a flat
		// platform 9m across would have an invisible 9m sphere on it. Squashed spheres likewise collide as round balls.
		// Swap both for a convex mesh of their actual shape.
		private static void MatchColliderToShape(GameObject prim, PrimitiveType type, Vector3 scale)
		{
			var squashedSphere = type == PrimitiveType.Sphere && !(Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.y, scale.z));

			if (type != PrimitiveType.Cylinder && !squashedSphere)
				return;

			Object.DestroyImmediate(prim.GetComponent<Collider>());

			var meshCollider = prim.AddComponent<MeshCollider>();
			meshCollider.sharedMesh = prim.GetComponent<MeshFilter>().sharedMesh;
			meshCollider.convex = true;
		}

		public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Vector3 localPosition, Vector3 localScale, Material material, bool collider = false, Vector3? euler = null)
		{
			var meshObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
			meshObject.transform.SetParent(parent, false);
			meshObject.transform.localPosition = localPosition;
			meshObject.transform.localScale = localScale;
			meshObject.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);

			meshObject.GetComponent<MeshFilter>().sharedMesh = mesh;
			meshObject.GetComponent<MeshRenderer>().sharedMaterial = material;

			if (collider)
				meshObject.AddComponent<MeshCollider>().convex = true;

			if (parent != null)
				meshObject.layer = parent.gameObject.layer;

			return meshObject;
		}

		public static void SetLayer(GameObject root, int layer)
		{
			foreach (var child in root.GetComponentsInChildren<Transform>(true))
				child.gameObject.layer = layer;
		}

		public static Light PointLight(Transform parent, Vector3 localPosition, Color colour, float range, float intensity)
		{
			var light = Node("Light", parent, localPosition).AddComponent<Light>();
			light.type = LightType.Point;
			light.color = colour;
			light.range = range;
			light.intensity = intensity;
			light.shadows = LightShadows.None;

			return light;
		}

		public static SphereCollider Trigger(GameObject target, float radius, Vector3 centre = default)
		{
			var trigger = target.AddComponent<SphereCollider>();
			trigger.isTrigger = true;
			trigger.radius = radius;
			trigger.center = centre;

			return trigger;
		}

		public static void Set(Object target, string property, Object value)
		{
			var serialized = new SerializedObject(target);
			serialized.FindProperty(property).objectReferenceValue = value;
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		public static void SetValue(Object target, string property, float value)
		{
			var serialized = new SerializedObject(target);
			var found = serialized.FindProperty(property);

			if (found.propertyType == SerializedPropertyType.Integer)
				found.intValue = (int)value;
			else
				found.floatValue = value;

			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		public static void SetBool(Object target, string property, bool value)
		{
			var serialized = new SerializedObject(target);
			serialized.FindProperty(property).boolValue = value;
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		public static void SetString(Object target, string property, string value)
		{
			var serialized = new SerializedObject(target);
			serialized.FindProperty(property).stringValue = value;
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		public static void SetVector(Object target, string property, Vector3 value)
		{
			var serialized = new SerializedObject(target);
			serialized.FindProperty(property).vector3Value = value;
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		public static void SetColour(Object target, string property, Color value)
		{
			var serialized = new SerializedObject(target);
			serialized.FindProperty(property).colorValue = value;
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		public static GameObject SavePrefab(GameObject root, string path)
		{
			EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));

			var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);

			Object.DestroyImmediate(root);

			return prefab;
		}

		#endregion

		#region Art slots

		public const string ArtSetPath = "Assets/Resources/ArtSet.asset";

		private static readonly SortedDictionary<string, string> artSlots = new();

		/// <summary>
		/// An Art Set slot with no placeholder in the scene, read by a script at play (the sky's presets).
		/// </summary>
		public static void NamedSlot(string key, string note) => artSlots[key] = note;

		/// <summary>
		/// Gives a placeholder an Art Set slot. The placeholder renderers are hidden while the slot has art;
		/// everything under an excluded transform (a harvestable herb, a sign's words) always shows.
		/// </summary>
		public static void ArtSlot(GameObject target, string key, string note, params Transform[] keepVisible)
		{
			var placeholder = target.GetComponentsInChildren<Renderer>(true)
				.Where(renderer => renderer is not ParticleSystemRenderer)
				.Where(renderer => !keepVisible.Any(kept => kept != null && renderer.transform.IsChildOf(kept)))
				.ToArray();

			var swap = target.GetComponent<ArtSwap>() ?? target.AddComponent<ArtSwap>();
			swap.Configure(key, placeholder);

			artSlots[key] = note;
		}

		/// <summary>
		/// Same, for a prefab asset already saved.
		/// </summary>
		public static void ArtSlotOnPrefab(GameObject prefab, string childPath, string key, string note, string keepVisibleName = null)
		{
			var path = AssetDatabase.GetAssetPath(prefab);
			var contents = PrefabUtility.LoadPrefabContents(path);

			try
			{
				var target = string.IsNullOrEmpty(childPath) ? contents : contents.transform.Find(childPath).gameObject;

				var kept = string.IsNullOrEmpty(keepVisibleName)
					? new Transform[0]
					: contents.GetComponentsInChildren<Transform>(true).Where(child => child.name == keepVisibleName).ToArray();

				ArtSlot(target, key, note, kept);

				PrefabUtility.SaveAsPrefabAsset(contents, path);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}
		}

		/// <summary>
		/// Gives every renderer in the open scene that uses one of these materials a surface slot, which can paint
		/// them with an Art Set material instead. Meshes and collision stay as they are.
		/// </summary>
		public static void SurfaceSlot(Transform parent, string key, string note, params Material[] materials)
		{
			var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
				.Where(renderer => materials.Contains(renderer.sharedMaterial))
				.Cast<Renderer>()
				.ToArray();

			if (renderers.Length == 0)
				return;

			var holder = new GameObject($"Surface ({key})");
			holder.transform.SetParent(parent, false);
			holder.AddComponent<ArtMaterialSwap>().Configure(key, renderers);

			artSlots[key] = note;
		}

		/// <summary>
		/// Makes sure the Art Set lists every slot the builders made. Never touches an existing entry's art.
		/// </summary>
		public static void SyncArtSet()
		{
			var set = LoadOrCreate<ArtSet>(ArtSetPath);

			foreach (var (key, note) in artSlots)
			{
				var entry = set.Entries.FirstOrDefault(existing => existing.Key == key);

				if (entry == null)
					set.Entries.Add(new ArtEntry { Key = key, Note = note });
				else
					entry.Note = note;
			}

			set.Entries = set.Entries.OrderBy(entry => entry.Key).ToList();
			set.NotifyChanged();

			EditorUtility.SetDirty(set);
			AssetDatabase.SaveAssets();
		}

		#endregion
	}
}
