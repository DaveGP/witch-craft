using UnityEngine;


namespace Data.Input
{
	public readonly struct CameraInput
	{
		public readonly Vector2 Look;

		public readonly float Zoom;

		/// <summary>
		/// Turning the view by key: -1 left to 1 right.
		/// </summary>
		public readonly float Turn;

		public CameraInput(Vector2 look, float zoom, float turn)
		{
			Look = look;
			Zoom = zoom;
			Turn = turn;
		}
	}
}
