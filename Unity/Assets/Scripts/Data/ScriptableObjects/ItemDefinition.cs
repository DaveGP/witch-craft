using Data.Enums;
using UnityEngine;

namespace Data.ScriptableObjects
{
	[CreateAssetMenu(fileName = "Item", menuName = "Witch Craft/Item", order = 10)]
	public class ItemDefinition : ScriptableObject
	{
		public string Id;

		public string DisplayName;

		public ItemCategory Category;

		public Color Color = Color.white;

		[TextArea] public string Description;

		[Header("Potion")]
		public PotionEffect PotionEffect;

		public float PotionDuration = 60f;
	}
}
