using System.Collections;
using UnityEngine;

namespace World
{
	// Shrinks a world-space label until it fits across its sign, so the words never run off the edge of whatever
	// art the sign wears
	[RequireComponent(typeof(TextMesh))]
	public class FitText : MonoBehaviour
	{
		[SerializeField] private float maxWidth = 0.8f;

		private IEnumerator Start()
		{
			// The text's mesh is built by the first frame
			yield return null;

			var bounds = GetComponent<MeshRenderer>().bounds;
			var right = transform.right;

			// The bounds are axis-aligned, so read the width along whichever way the sign faces
			var width = Mathf.Abs(bounds.size.x * right.x) + Mathf.Abs(bounds.size.z * right.z);

			if (width > maxWidth)
				transform.localScale *= maxWidth / width;
		}
	}
}
