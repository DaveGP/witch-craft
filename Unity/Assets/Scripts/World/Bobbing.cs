using UnityEngine;

namespace World
{
	// Gentle float and spin for pickups, runes and wisps.
	public class Bobbing : MonoBehaviour
	{
		[SerializeField] private float height = 0.15f;

		[SerializeField] private float speed = 1.5f;

		[SerializeField] private float spin = 30f;

		private Vector3 start;

		private float offset;

		private void Awake()
		{
			start = transform.localPosition;
			offset = Random.value * 10f;
		}

		private void Update()
		{
			transform.localPosition = start + Vector3.up * (Mathf.Sin((Time.time + offset) * speed) * height);
			transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
		}
	}
}
