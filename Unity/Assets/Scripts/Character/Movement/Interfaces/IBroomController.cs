using UnityEngine;

namespace Character.Movement.Interfaces
{
	public interface IBroomController : IBroomState
	{
		public void Toggle();

		/// <summary>
		/// Flies in a world direction (magnitude 0-1), climbing or diving with vertical (-1 to 1).
		/// </summary>
		public void Fly(Vector3 direction, float vertical, bool boost);

		/// <summary>
		/// Tops up the broom's charge, for updrafts and potions.
		/// </summary>
		public void Recharge(float seconds);

		/// <summary>
		/// Lets the broom climb above its hover ceiling for a moment, for updrafts.
		/// </summary>
		public void Uplift(float seconds);
	}
}
