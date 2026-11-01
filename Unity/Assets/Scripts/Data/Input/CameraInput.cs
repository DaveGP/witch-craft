using UnityEngine;


namespace Data.Input
{
	public readonly struct CameraInput
	{
		public readonly Vector2 Look;

		public readonly float Zoom;

		public CameraInput(Vector2 look, float zoom)
		{
			Look = look;
			Zoom = zoom;
		}
	}
}
