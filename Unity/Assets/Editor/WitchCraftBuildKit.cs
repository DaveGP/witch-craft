using System.Collections.Generic;
using System.IO;
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

			if (parent != null)
				prim.layer = parent.gameObject.layer;

			return prim;
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
	}
}
