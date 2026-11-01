using System.Collections.Generic;
using UnityEngine;

namespace Art
{
	// Sets Animator parameters only when the controller has them, so any rig can be dropped in and wired up gradually.
	public class AnimatorParameters
	{
		private readonly Animator animator;

		private readonly HashSet<int> available = new();

		public AnimatorParameters(Animator animator)
		{
			this.animator = animator;

			if (animator == null || animator.runtimeAnimatorController == null)
				return;

			foreach (var parameter in animator.parameters)
				available.Add(parameter.nameHash);
		}

		public bool Has(int id) => available.Contains(id);

		public void SetFloat(int id, float value)
		{
			if (Has(id))
				animator.SetFloat(id, value);
		}

		public void SetInteger(int id, int value)
		{
			if (Has(id))
				animator.SetInteger(id, value);
		}

		public void SetBool(int id, bool value)
		{
			if (Has(id))
				animator.SetBool(id, value);
		}

		public void SetTrigger(int id)
		{
			if (Has(id))
				animator.SetTrigger(id);
		}
	}
}
