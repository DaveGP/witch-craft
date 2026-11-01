using UnityEngine;


namespace Camera
{
	using Interfaces;


	public class CharacterCameraTarget : MonoBehaviour, ICameraTarget
	{
		[SerializeField] private Transform cameraTarget;
		public Vector3 Position => cameraTarget.position;
		public Quaternion Rotation => cameraTarget.rotation;
	}
}
