using Data.Enums;
using DependencyInjection;
using Data.ScriptableObjects;
using UnityEngine;


namespace Home
{
	// The patch of meadow the witch can build on. Structures snap to a grid of cells: floors and roofs to cell
	// centres, walls to the nearest cell edge, all stacked in storeys. Furniture sits freely on a finer grid.
	public class HomePlot : MonoBehaviour
	{
		public const float CellSize = 2f;

		public const float StoreyHeight = 2.5f;

		public const float FreeSnap = 0.25f;

		public int Cells { get; private set; } = 8;

		public float HalfExtent => Cells * CellSize * 0.5f;

		[SerializeField] private LineRenderer border;

		[SerializeField] private int maxStoreys = 3;

		public void SetCells(int cells)
		{
			Cells = cells;

			UpdateBorder();
		}

		public bool Contains(Vector3 point, float margin = 0f)
		{
			var local = point - transform.position;

			return Mathf.Abs(local.x) <= HalfExtent + margin && Mathf.Abs(local.z) <= HalfExtent + margin;
		}

		public Vector3 RandomPoint(float margin)
		{
			var extent = Mathf.Max(0.5f, HalfExtent - margin);

			return transform.position + new Vector3(Random.Range(-extent, extent), 0f, Random.Range(-extent, extent));
		}

		public bool Snap(BuildSnap snap, Vector3 point, int rotationSteps, out Vector3 position, out Quaternion rotation)
		{
			var local = point - transform.position;

			// Small lift so aiming at the top of a floor tile counts as that storey
			var storey = Mathf.Clamp(Mathf.FloorToInt((local.y + 0.35f) / StoreyHeight), 0, maxStoreys - 1);

			var y = storey * StoreyHeight;

			var cellX = Mathf.FloorToInt(local.x / CellSize);
			var cellZ = Mathf.FloorToInt(local.z / CellSize);

			var centre = new Vector3((cellX + 0.5f) * CellSize, y, (cellZ + 0.5f) * CellSize);

			switch (snap)
			{
				case BuildSnap.Wall:
					SnapWall(local, centre, out position, out rotation);
					break;

				case BuildSnap.Free:
					position = new Vector3(Round(local.x), local.y, Round(local.z));
					rotation = Quaternion.Euler(0f, rotationSteps * 45f, 0f);
					break;

				default:
					position = centre;
					rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
					break;
			}

			position += transform.position;

			return Contains(position, 0.01f);
		}

		private static void SnapWall(Vector3 local, Vector3 centre, out Vector3 position, out Quaternion rotation)
		{
			var offset = local - centre;

			// Walls are modelled along local X, so the north and south edges keep yaw 0
			if (Mathf.Abs(offset.x) > Mathf.Abs(offset.z))
			{
				position = centre + Vector3.right * (Mathf.Sign(offset.x) * CellSize * 0.5f);
				rotation = Quaternion.Euler(0f, 90f, 0f);
			}
			else
			{
				position = centre + Vector3.forward * (Mathf.Sign(offset.z) * CellSize * 0.5f);
				rotation = Quaternion.identity;
			}
		}

		private static float Round(float value) => Mathf.Round(value / FreeSnap) * FreeSnap;

		private void UpdateBorder()
		{
			if (border == null)
				return;

			const int segmentsPerSide = 24;

			var extent = HalfExtent;

			border.positionCount = segmentsPerSide * 4 + 1;
			border.loop = false;
			border.useWorldSpace = true;

			var corners = new[]
			{
				new Vector3(-extent, 0, -extent), new Vector3(extent, 0, -extent),
				new Vector3(extent, 0, extent), new Vector3(-extent, 0, extent)
			};

			var index = 0;

			for (var side = 0; side < 4; side++)
			for (var step = 0; step < segmentsPerSide; step++)
			{
				var point = Vector3.Lerp(corners[side], corners[(side + 1) % 4], step / (float)segmentsPerSide);
				border.SetPosition(index++, ProjectToGround(transform.position + point));
			}

			border.SetPosition(index, ProjectToGround(transform.position + corners[0]));
		}

		private static Vector3 ProjectToGround(Vector3 point)
		{
			if (Physics.Raycast(point + Vector3.up * 20f, Vector3.down, out var hit, 40f, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore))
				return hit.point + Vector3.up * 0.06f;

			return point;
		}
	}
}
