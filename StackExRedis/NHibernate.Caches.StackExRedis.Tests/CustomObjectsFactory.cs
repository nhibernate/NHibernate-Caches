using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Bytecode;

namespace NHibernate.Caches.StackExRedis.Tests
{
	public class CustomObjectsFactory : IObjectsFactory
	{
		private readonly Dictionary<System.Type, System.Type> _registeredTypes = new Dictionary<System.Type, System.Type>();
		private readonly Dictionary<System.Type, object> _registeredSingletons = new Dictionary<System.Type, object>();

		public void Register<TBaseType, TConcreteType>()
		{
			_registeredTypes.Add(typeof(TBaseType), typeof(TConcreteType));
		}

		public void RegisterSingleton<TBaseType>(TBaseType value)
		{
			_registeredSingletons.Add(typeof(TBaseType), value);
		}

		public object CreateInstance(System.Type type)
		{
			return _registeredSingletons.TryGetValue(type, out var singleton) 
				? singleton 
				: Activator.CreateInstance(_registeredTypes.TryGetValue(type, out var concreteType) ? concreteType : type);
		}

		public object CreateInstance(System.Type type, bool nonPublic)
		{
			throw new NotSupportedException();
		}

		public object CreateInstance(System.Type type, params object[] ctorArgs)
		{
			throw new NotSupportedException();
		}
	}
}
