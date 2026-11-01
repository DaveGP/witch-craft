using System;
using System.Collections.Generic;


namespace DependencyInjection
{
	public static class ServiceLocator
	{
		private static readonly Dictionary<Type, object> services = new();

		public static void Register<T>(T service) where T : class
		{
			services[typeof(T)] = service;
		}

		public static void Unregister<T>(T service) where T : class
		{
			if (services.TryGetValue(typeof(T), out var registered) && registered == service)
				services.Remove(typeof(T));
		}

		public static T Get<T>() where T : class
			=> services.TryGetValue(typeof(T), out var service) ? service as T : null;
	}
}
