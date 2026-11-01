using Data.ScriptableObjects;
using DependencyInjection;
using UnityEngine;

namespace Utilities
{
	// Code-built particle effects, so sparkles and puffs need no authored prefabs.
	public static class FxUtility
	{
		private static Material ParticleMaterial => ServiceLocator.Get<GameDatabase>()?.ParticleMaterial;

		public static void Burst(Vector3 position, Color color, int count = 24, float speed = 3f, float size = 0.18f)
		{
			var system = Create("Burst", position, color, size, false);

			var main = system.main;
			main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
			main.gravityModifier = -0.15f;
			main.stopAction = ParticleSystemStopAction.Destroy;

			var emission = system.emission;
			emission.rateOverTime = 0f;
			emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

			system.Play();
		}

		/// <summary>
		/// A looping column of rising motes, for updrafts and magical glows.
		/// </summary>
		public static ParticleSystem Loop(Transform parent, Vector3 localPosition, Color color, float rate, float speed, float radius, float size = 0.12f)
		{
			var system = Create("Loop", parent.TransformPoint(localPosition), color, size, true);
			system.transform.SetParent(parent, true);

			var main = system.main;
			main.startSpeed = speed;
			main.startLifetime = 2.5f;

			var emission = system.emission;
			emission.rateOverTime = rate;

			var shape = system.shape;
			shape.shapeType = ParticleSystemShapeType.Circle;
			shape.radius = radius;
			shape.rotation = new Vector3(-90f, 0f, 0f);

			system.Play();

			return system;
		}

		private static ParticleSystem Create(string name, Vector3 position, Color color, float size, bool loop)
		{
			var gameObject = new GameObject($"Fx_{name}");
			gameObject.transform.position = position;

			var system = gameObject.AddComponent<ParticleSystem>();
			system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

			var main = system.main;
			main.duration = 1f;
			main.loop = loop;
			main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
			main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.6f));
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.playOnAwake = false;

			var shape = system.shape;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 0.3f;

			var colorOverLifetime = system.colorOverLifetime;
			colorOverLifetime.enabled = true;
			var gradient = new Gradient();
			gradient.SetKeys(
				new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
				new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
			colorOverLifetime.color = gradient;

			var renderer = system.GetComponent<ParticleSystemRenderer>();
			renderer.material = ParticleMaterial;

			return system;
		}
	}
}
