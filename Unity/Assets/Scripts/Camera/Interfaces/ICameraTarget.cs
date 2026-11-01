using UnityEngine;


namespace Camera.Interfaces
{
	public interface ICameraTarget
	{
		public Vector3 Position { get; }
		public Quaternion Rotation { get; }
	}
}
